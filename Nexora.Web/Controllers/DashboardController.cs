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
        var from7 = DateTime.UtcNow.Date.AddDays(-6);

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

        // Sales in the last 7 days (daily)
        var salesRaw = await _db.Orders
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId && x.CreatedAtUtc >= from7)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Total),
                Orders = g.Count()
            })
            .ToListAsync();

        var sales7 = new List<DailySalesVm>();
        for (var i = 0; i < 7; i++)
        {
            var day = from7.AddDays(i);
            var row = salesRaw.FirstOrDefault(x => x.Date == day);
            sales7.Add(new DailySalesVm
            {
                DateUtc = day,
                Revenue = row?.Revenue ?? 0m,
                OrdersCount = row?.Orders ?? 0
            });
        }

        var vm = new DashboardVm
        {
            RevenueLast30Days = revenue,
            OrdersLast30Days = ordersCount,
            CustomersCount = customersCount,
            SalesLast7Days = sales7,
            LowStockProducts = lowStock
        };

        return View(vm);
    }
}