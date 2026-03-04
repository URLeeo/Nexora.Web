using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;

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
        var items = await _db.ContactMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
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
        var item = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        item.IsDeleted = true;
        item.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
