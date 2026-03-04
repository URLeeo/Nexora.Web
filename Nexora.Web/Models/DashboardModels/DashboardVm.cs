namespace Nexora.Web.Models;

public class DashboardVm
{
    public decimal RevenueLast30Days { get; set; }
    public int OrdersLast30Days { get; set; }
    public int CustomersCount { get; set; }

    public List<DailySalesVm> SalesLast7Days { get; set; } = new();

    public List<LowStockVm> LowStockProducts { get; set; } = new();
}

public class DailySalesVm
{
    public DateTime DateUtc { get; set; }
    public decimal Revenue { get; set; }
    public int OrdersCount { get; set; }
}

public class LowStockVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int StockOnHand { get; set; }
    public int Threshold { get; set; }
}