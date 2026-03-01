using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Extensions;
using Nexora.Web.Models.ImportModels;

namespace Nexora.Web.Controllers;

[Authorize]
public class ImportsController : Controller
{
    private readonly AppDbContext _db;

    public ImportsController(AppDbContext db)
    {
        _db = db;
    }

    // Upload page
    [HttpGet]
    public IActionResult Orders()
    {
        return View();
    }

    // ✅ Download default template
    [HttpGet]
    public IActionResult OrdersTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Orders");

        // Headers
        var headers = new[]
        {
            "CustomerFullName","CustomerPhone","ProductSku","ProductName","Qty","UnitPrice","Discount","Tax"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);

        // Example rows (optional)
        ws.Cell(2, 1).Value = "Ali Veli";
        ws.Cell(2, 2).Value = "0501234567";
        ws.Cell(2, 3).Value = "SKU-001";
        ws.Cell(2, 5).Value = 2;
        ws.Cell(2, 6).Value = 15.5;

        ws.Cell(3, 1).Value = "Ali Veli";
        ws.Cell(3, 2).Value = "0501234567";
        ws.Cell(3, 3).Value = "SKU-002";
        ws.Cell(3, 5).Value = 1;
        ws.Cell(3, 6).Value = 8;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Nexora_Order_Import_Template.xlsx"
        );
    }

    // ✅ Preview (parse + validate, no DB writes)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrdersPreview(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please upload a valid Excel file (.xlsx).";
            return RedirectToAction(nameof(Orders));
        }

        var orgId = User.GetOrganizationId();

        // Read excel rows
        var rows = await ReadRowsAsync(file);

        if (rows.Count == 0)
        {
            TempData["Error"] = "Excel file has no data rows.";
            return RedirectToAction(nameof(Orders));
        }

        // Group rows into orders by customer key
        var grouped = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerFullName))
            .GroupBy(r => $"{r.CustomerFullName}||{r.CustomerPhone}");

        // Preload products for validation (fast)
        var activeProducts = await _db.Products
    .AsNoTracking()
    .Where(x => x.OrganizationId == orgId && x.IsActive)
    .Select(x => new ProductLookup(x.Id, x.Name, x.Sku, x.StockOnHand))
    .ToListAsync();

        var preview = new OrdersImportPreviewVm();

        foreach (var g in grouped)
        {
            var first = g.First();
            var orderVm = new OrderImportPreviewVm
            {
                CustomerFullName = first.CustomerFullName.Trim(),
                CustomerPhone = first.CustomerPhone?.Trim()
            };

            decimal subtotal = 0;
            decimal discount = 0;
            decimal tax = 0;

            foreach (var r in g)
            {
                var item = new OrderImportItemPreviewVm
                {
                    ProductSku = r.ProductSku?.Trim(),
                    ProductName = r.ProductName?.Trim(),
                    Quantity = r.Qty,
                    UnitPrice = r.UnitPrice
                };

                // Basic validations
                if (item.Quantity <= 0)
                    item.Errors.Add("Qty must be > 0.");

                if (item.UnitPrice < 0)
                    item.Errors.Add("UnitPrice cannot be negative.");

                // Find product
                var product = FindProduct(activeProducts, item.ProductSku, item.ProductName);

                if (product == null)
                {
                    item.Errors.Add("Product not found (check SKU or Name).");
                }
                else
                {
                    item.ProductId = product.Id;
                    item.ResolvedProductName = product.Name;
                    item.ResolvedProductSku = product.Sku;

                    if (product.StockOnHand < item.Quantity)
                        item.Errors.Add($"Not enough stock. Available: {product.StockOnHand}");
                }

                item.LineTotal = item.UnitPrice * item.Quantity;
                subtotal += item.LineTotal;

                orderVm.Items.Add(item);

                discount = r.Discount; // same discount/tax can be repeated; we’ll just take last one
                tax = r.Tax;
            }

            orderVm.Subtotal = subtotal;
            orderVm.DiscountAmount = discount;
            orderVm.TaxAmount = tax;

            var total = subtotal - discount + tax;
            orderVm.Total = total < 0 ? 0 : total;

            // Order-level validation
            if (orderVm.Items.Count == 0)
                orderVm.Errors.Add("Order has no items.");

            if (orderVm.Items.Any(i => i.Errors.Count > 0))
                orderVm.Errors.Add("Order has invalid items. Fix errors before importing.");

            preview.Orders.Add(orderVm);
        }

        if (preview.Orders.Count == 0)
        {
            TempData["Error"] = "No valid orders found in Excel (check CustomerFullName).";
            return RedirectToAction(nameof(Orders));
        }

        // Serialize preview -> hidden field for commit
        preview.PayloadJson = JsonSerializer.Serialize(preview.Orders);

        return View("OrdersPreview", preview);
    }

    // ✅ Commit (write to DB with transaction)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrdersCommit(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            TempData["Error"] = "Missing preview payload.";
            return RedirectToAction(nameof(Orders));
        }

        List<OrderImportPreviewVm>? orders;
        try
        {
            orders = JsonSerializer.Deserialize<List<OrderImportPreviewVm>>(payloadJson);
        }
        catch
        {
            TempData["Error"] = "Invalid payload format.";
            return RedirectToAction(nameof(Orders));
        }

        if (orders == null || orders.Count == 0)
        {
            TempData["Error"] = "No orders to import.";
            return RedirectToAction(nameof(Orders));
        }

        var orgId = User.GetOrganizationId();

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var o in orders)
            {
                // Safety: block if any errors (preview should already prevent this)
                if (o.Errors.Count > 0 || o.Items.Any(i => i.Errors.Count > 0))
                    throw new Exception($"Cannot import. Fix errors for customer: {o.CustomerFullName}");

                // Customer: find or create (phone optional)
                var customer = await _db.Customers.FirstOrDefaultAsync(x =>
                    x.OrganizationId == orgId &&
                    x.FullName == o.CustomerFullName &&
                    x.Phone == o.CustomerPhone);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        OrganizationId = orgId,
                        FullName = o.CustomerFullName,
                        Phone = o.CustomerPhone
                    };
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync();
                }

                // Load needed products (tracked)
                var productIds = o.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _db.Products
                    .Where(x => x.OrganizationId == orgId && productIds.Contains(x.Id))
                    .ToListAsync();

                if (products.Count != productIds.Count)
                    throw new Exception("One or more products are invalid (changed since preview).");

                // Re-check stock (important)
                foreach (var it in o.Items)
                {
                    var p = products.First(x => x.Id == it.ProductId);
                    if (p.StockOnHand < it.Quantity)
                        throw new Exception($"Not enough stock for {p.Name}. Available: {p.StockOnHand}");
                }

                // Create order
                var order = new Order
                {
                    OrganizationId = orgId,
                    CustomerId = customer.Id,
                    OrderNo = $"IMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Status = OrderStatus.Paid,
                    Subtotal = o.Subtotal,
                    DiscountAmount = o.DiscountAmount,
                    TaxAmount = o.TaxAmount,
                    Total = o.Total
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // Items + stock + movements
                foreach (var it in o.Items)
                {
                    var p = products.First(x => x.Id == it.ProductId);

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = p.Id,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice,
                        LineTotal = it.LineTotal
                    });

                    p.StockOnHand -= it.Quantity;

                    _db.StockMovements.Add(new StockMovement
                    {
                        OrganizationId = orgId,
                        ProductId = p.Id,
                        Type = StockMovementType.Sale,
                        QuantityChange = -it.Quantity,
                        RelatedOrderId = order.Id,
                        Note = $"Imported: {order.OrderNo}"
                    });
                }

                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            TempData["Success"] = $"Imported {orders.Count} orders successfully.";
            return RedirectToAction(nameof(Orders));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            throw;
        }
    }

    // ---------- helpers ----------
    private sealed record ProductLookup(Guid Id, string Name, string? Sku, int StockOnHand);

    private static async Task<List<OrderImportRowVm>> ReadRowsAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range == null) return new();

        var rows = range.RowsUsed().Skip(1); // skip header

        var list = new List<OrderImportRowVm>();

        foreach (var r in rows)
        {
            // Columns:
            // 1 FullName, 2 Phone, 3 SKU, 4 Name, 5 Qty, 6 UnitPrice, 7 Discount, 8 Tax
            var vm = new OrderImportRowVm
            {
                CustomerFullName = r.Cell(1).GetString(),
                CustomerPhone = r.Cell(2).GetString(),
                ProductSku = r.Cell(3).GetString(),
                ProductName = r.Cell(4).GetString(),
                Qty = ReadInt(r.Cell(5)),
                UnitPrice = ReadDecimal(r.Cell(6)),
                Discount = ReadDecimal(r.Cell(7)),
                Tax = ReadDecimal(r.Cell(8))
            };

            // ignore fully empty rows
            var allEmpty =
                string.IsNullOrWhiteSpace(vm.CustomerFullName) &&
                string.IsNullOrWhiteSpace(vm.CustomerPhone) &&
                string.IsNullOrWhiteSpace(vm.ProductSku) &&
                string.IsNullOrWhiteSpace(vm.ProductName) &&
                vm.Qty == 0 && vm.UnitPrice == 0 && vm.Discount == 0 && vm.Tax == 0;

            if (!allEmpty)
                list.Add(vm);
        }

        return list;
    }

    private static int ReadInt(IXLCell cell)
    {
        if (cell.IsEmpty()) return 0;

        if (cell.DataType == XLDataType.Number)
            return (int)cell.GetDouble();

        var s = cell.GetString();
        return int.TryParse(s, out var v) ? v : 0;
    }

    private static decimal ReadDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return 0m;

        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        var s = cell.GetString();
        return decimal.TryParse(s, out var v) ? v : 0m;
    }

    private static ProductLookup? FindProduct(List<ProductLookup> products, string? sku, string? name)
    {
        if (!string.IsNullOrWhiteSpace(sku))
        {
            var p = products.FirstOrDefault(x => x.Sku == sku);
            if (p != null) return p;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var p = products.FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (p != null) return p;
        }

        return null;
    }
}