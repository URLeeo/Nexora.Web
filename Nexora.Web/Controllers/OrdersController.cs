using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Extensions;
using Nexora.Web.Models.OrderModels;
using System.Text;

namespace Nexora.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var orgId = User.GetOrganizationId();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.OrganizationId == orgId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookups();

        var vm = new OrderCreateVm();
        vm.Items.Add(new OrderCreateItemVm());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateVm vm)
    {
        var orgId = User.GetOrganizationId();

        // Keep only valid rows
        vm.Items = vm.Items
            .Where(i => i.ProductId != Guid.Empty && i.Quantity > 0)
            .ToList();

        if (vm.Items.Count == 0)
            ModelState.AddModelError("", "Add at least 1 product item.");

        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(vm);
        }

        if (vm.CustomerId.HasValue)
        {
            var customerOk = await _db.Customers
                .AnyAsync(x => x.OrganizationId == orgId && x.Id == vm.CustomerId.Value);

            if (!customerOk)
            {
                ModelState.AddModelError("", "Invalid customer.");
                await LoadLookups();
                return View(vm);
            }
        }

        // Validate products
        var productIds = vm.Items.Select(x => x.ProductId).Distinct().ToList();

        var products = await _db.Products
            .Where(x => x.OrganizationId == orgId && productIds.Contains(x.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            ModelState.AddModelError("", "One or more products are invalid.");
            await LoadLookups();
            return View(vm);
        }

        // Validate stock availability
        foreach (var item in vm.Items)
        {
            var p = products.First(x => x.Id == item.ProductId);

            if (p.StockOnHand < item.Quantity)
            {
                ModelState.AddModelError("", $"Not enough stock for: {p.Name}. Available: {p.StockOnHand}");
                await LoadLookups();
                return View(vm);
            }
        }

        // Calculate totals
        var subtotal = 0m;
        foreach (var item in vm.Items)
            subtotal += item.UnitPrice * item.Quantity;

        var total = subtotal - vm.DiscountAmount + vm.TaxAmount;
        if (total < 0) total = 0;

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                OrganizationId = orgId,
                CustomerId = vm.CustomerId,
                OrderNo = GenerateOrderNo(),
                Status = OrderStatus.Paid,
                Subtotal = subtotal,
                DiscountAmount = vm.DiscountAmount,
                TaxAmount = vm.TaxAmount,
                Total = total
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in vm.Items)
            {
                var p = products.First(x => x.Id == item.ProductId);
                var lineTotal = item.UnitPrice * item.Quantity;

                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal
                });

                // stock decrease
                p.StockOnHand -= item.Quantity;

                // stock movement log
                _db.StockMovements.Add(new StockMovement
                {
                    OrganizationId = orgId,
                    ProductId = p.Id,
                    Type = StockMovementType.Sale,
                    QuantityChange = -item.Quantity,
                    RelatedOrderId = order.Id,
                    Note = $"Sale: {order.OrderNo}"
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Order created: {order.OrderNo}";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch
        {
            // no commit => rollback
            TempData["Error"] = "Failed to create order. Please try again.";
            throw;
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var orgId = User.GetOrganizationId();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var orgId = User.GetOrganizationId();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.OrganizationId == orgId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderNo,Customer,Status,Subtotal,Discount,Tax,Total,CreatedAtUtc");

        static string Csv(string? s)
        {
            s ??= "";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }

        foreach (var o in orders)
        {
            sb.Append(Csv(o.OrderNo)); sb.Append(',');
            sb.Append(Csv(o.Customer?.FullName ?? "")); sb.Append(',');
            sb.Append(Csv(o.Status.ToString())); sb.Append(',');
            sb.Append(o.Subtotal); sb.Append(',');
            sb.Append(o.DiscountAmount); sb.Append(',');
            sb.Append(o.TaxAmount); sb.Append(',');
            sb.Append(o.Total); sb.Append(',');
            sb.AppendLine(o.CreatedAtUtc.ToString("O"));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"nexora-orders-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private async Task LoadLookups()
    {
        var orgId = User.GetOrganizationId();

        ViewBag.Customers = await _db.Customers
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        ViewBag.Products = await _db.Products
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    private static string GenerateOrderNo()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}