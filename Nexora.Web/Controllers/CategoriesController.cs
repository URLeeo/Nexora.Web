using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Extensions;

namespace Nexora.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var orgId = User.GetOrganizationId();
        var items = await _db.Categories
            .Where(x => x.OrganizationId == orgId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("", "Name is required.");
            return View();
        }

        var orgId = User.GetOrganizationId();
        var exists = await _db.Categories.AnyAsync(x => x.OrganizationId == orgId && x.Name == name.Trim());
        if (exists)
        {
            ModelState.AddModelError("", "Category already exists.");
            return View();
        }

        _db.Categories.Add(new Category
        {
            OrganizationId = orgId,
            Name = name.Trim()
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("", "Name is required.");
            return View(item);
        }

        item.Name = name.Trim();
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item == null) return NotFound();

        item.IsDeleted = true;
        item.DeletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}