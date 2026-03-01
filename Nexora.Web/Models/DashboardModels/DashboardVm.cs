namespace Nexora.Web.Models;

public class DashboardVm
{
    public decimal RevenueLast30Days { get; set; }
    public int OrdersLast30Days { get; set; }
    public int CustomersCount { get; set; }

    public List<LowStockVm> LowStockProducts { get; set; } = new();
}

public class LowStockVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int StockOnHand { get; set; }
    public int Threshold { get; set; }
}