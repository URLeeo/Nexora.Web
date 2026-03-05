using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Extensions;
using Nexora.Web.Models.App;

namespace Nexora.Web.Controllers;

[Authorize]
public class ContactMessagesController : Controller
{
    private readonly AppDbContext _db;

    public ContactMessagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orgId = User.GetOrganizationId();

        var messages = await _db.ContactMessages
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        var unreadCount = messages.Count(x => !x.IsRead);

        var lowStock = await _db.Products
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.StockOnHand <= p.LowStockThreshold)
            .OrderBy(p => p.StockOnHand)
            .ThenBy(p => p.Name)
            .Take(200)
            .ToListAsync();

        var vm = new MessagesInboxVm
        {
            ContactMessages = messages,
            UnreadContactCount = unreadCount,
            LowStockProducts = lowStock
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item is null) return NotFound();

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgId = User.GetOrganizationId();
        var item = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId);
        if (item is null) return NotFound();

        item.IsDeleted = true;
        item.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
