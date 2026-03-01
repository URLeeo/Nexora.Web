using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data;
using Nexora.Web.Extensions;
using Nexora.Web.Models;

namespace Nexora.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var orgId = User.GetOrganizationId();
        var from = DateTime.UtcNow.AddDays(-30);

        var revenue = await _db.Orders
            .Where(x => x.OrganizationId == orgId && x.CreatedAtUtc >= from)
            .SumAsync(x => (decimal?)x.Total) ?? 0m;

        var ordersCount = await _db.Orders
            .CountAsync(x => x.OrganizationId == orgId && x.CreatedAtUtc >= from);

        var customersCount = await _db.Customers
            .CountAsync(x => x.OrganizationId == orgId);

        var lowStock = await _db.Products
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId && x.IsActive && x.StockOnHand <= x.LowStockThreshold)
            .OrderBy(x => x.StockOnHand)
            .Take(10)
            .Select(x => new LowStockVm
            {
                Id = x.Id,
                Name = x.Name,
                StockOnHand = x.StockOnHand,
                Threshold = x.LowStockThreshold
            })
            .ToListAsync();

        var vm = new DashboardVm
        {
            RevenueLast30Days = revenue,
            OrdersLast30Days = ordersCount,
            CustomersCount = customersCount,
            LowStockProducts = lowStock
        };

        return View(vm);
    }
}