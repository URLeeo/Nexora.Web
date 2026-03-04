using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private readonly IWebHostEnvironment _env;

    public ProductsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
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

        // Optional image upload
        if (vm.ImageFile != null)
        {
            var savedUrl = await SaveProductImageAsync(vm.ImageFile);
            if (savedUrl == null)
            {
                ModelState.AddModelError("", "Invalid image. Please upload a PNG/JPG/WEBP up to 2MB.");
                await LoadCategories();
                return View(vm);
            }

            product.ImageUrl = savedUrl;
        }

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
            IsActive = product.IsActive,
            CurrentImageUrl = product.ImageUrl
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

        // Optional image upload (replace)
        if (vm.ImageFile != null)
        {
            var savedUrl = await SaveProductImageAsync(vm.ImageFile);
            if (savedUrl == null)
            {
                ModelState.AddModelError("", "Invalid image. Please upload a PNG/JPG/WEBP up to 2MB.");
                await LoadCategories();
                vm.CurrentImageUrl = product.ImageUrl;
                return View(vm);
            }

            product.ImageUrl = savedUrl;
        }

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

    private async Task<string?> SaveProductImageAsync(IFormFile file)
    {
        // Basic validation (MVP)
        const long maxBytes = 2 * 1024 * 1024; // 2MB
        if (file.Length <= 0 || file.Length > maxBytes) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
        if (!allowed.Contains(ext)) return null;

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, name);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(fs);

        return $"/uploads/products/{name}";
    }
}