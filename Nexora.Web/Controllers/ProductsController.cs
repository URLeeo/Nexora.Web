using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Extensions;
using Nexora.Web.Models;
using Nexora.Web.Models.ProductModels;

namespace Nexora.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? q = null)
    {
        var orgId = User.GetOrganizationId();

        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Name.Contains(q) || (x.Sku != null && x.Sku.Contains(q)));

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Q = q;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new ProductCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return View(vm);
        }

        var orgId = User.GetOrganizationId();

        var categoryOk = await _db.Categories.AnyAsync(x => x.OrganizationId == orgId && x.Id == vm.CategoryId);
        if (!categoryOk)
        {
            ModelState.AddModelError("", "Invalid category.");
            await LoadCategories();
            return View(vm);
        }

        var product = new Product
        {
            OrganizationId = orgId,
            CategoryId = vm.CategoryId,
            Name = vm.Name.Trim(),
            Sku = string.IsNullOrWhiteSpace(vm.Sku) ? null : vm.Sku.Trim(),
            SalePrice = vm.SalePrice,
            CostPrice = vm.CostPrice,
            StockOnHand = vm.StockOnHand,
            LowStockThreshold = vm.LowStockThreshold,
            IsActive = vm.IsActive
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var orgId = User.GetOrganizationId();

        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);

        if (product == null) return NotFound();

        var vm = new ProductEditVm
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            CategoryId = product.CategoryId,
            SalePrice = product.SalePrice,
            CostPrice = product.CostPrice,
            StockOnHand = product.StockOnHand,
            LowStockThreshold = product.LowStockThreshold,
            IsActive = product.IsActive
        };

        await LoadCategories();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return View(vm);
        }

        var orgId = User.GetOrganizationId();

        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == vm.Id && x.OrganizationId == orgId);

        if (product == null) return NotFound();

        var categoryOk = await _db.Categories.AnyAsync(x => x.OrganizationId == orgId && x.Id == vm.CategoryId);
        if (!categoryOk)
        {
            ModelState.AddModelError("", "Invalid category.");
            await LoadCategories();
            return View(vm);
        }

        product.Name = vm.Name.Trim();
        product.Sku = string.IsNullOrWhiteSpace(vm.Sku) ? null : vm.Sku.Trim();
        product.CategoryId = vm.CategoryId;
        product.SalePrice = vm.SalePrice;
        product.CostPrice = vm.CostPrice;
        product.StockOnHand = vm.StockOnHand;
        product.LowStockThreshold = vm.LowStockThreshold;
        product.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgId = User.GetOrganizationId();

        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);

        if (product == null) return NotFound();

        product.IsDeleted = true;
        product.DeletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategories()
    {
        var orgId = User.GetOrganizationId();
        var cats = await _db.Categories
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.Categories = cats;
    }
}