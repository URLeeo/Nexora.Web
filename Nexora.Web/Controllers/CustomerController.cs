using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Data.Models;
using Nexora.Web.Extensions;

namespace Nexora.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? q = null)
    {
        var orgId = User.GetOrganizationId();

        var query = _db.Customers
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x =>
                x.FullName.Contains(q) ||
                (x.Phone != null && x.Phone.Contains(q)) ||
                (x.Email != null && x.Email.Contains(q)));

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Q = q;
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer vm)
    {
        if (string.IsNullOrWhiteSpace(vm.FullName))
        {
            ModelState.AddModelError("", "Full name is required.");
            return View(vm);
        }

        var orgId = User.GetOrganizationId();

        var customer = new Customer
        {
            OrganizationId = orgId,
            FullName = vm.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
            Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim()
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Customer vm)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Customers.FirstOrDefaultAsync(x => x.Id == vm.Id && x.OrganizationId == orgId);
        if (item == null) return NotFound();

        if (string.IsNullOrWhiteSpace(vm.FullName))
        {
            ModelState.AddModelError("", "Full name is required.");
            return View(vm);
        }

        item.FullName = vm.FullName.Trim();
        item.Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim();
        item.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        item.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item == null) return NotFound();

        item.IsDeleted = true;
        item.DeletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}