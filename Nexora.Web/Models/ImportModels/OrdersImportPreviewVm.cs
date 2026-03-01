namespace Nexora.Web.Models.ImportModels;

public class OrdersImportPreviewVm
{
    public List<OrderImportPreviewVm> Orders { get; set; } = new();

    // for commit hidden input
    public string PayloadJson { get; set; } = string.Empty;

    public int OrdersCount => Orders.Count;
    public int ItemsCount => Orders.Sum(o => o.Items.Count);

    public int ErrorsCount =>
        Orders.Sum(o => o.Errors.Count) + Orders.Sum(o => o.Items.Sum(i => i.Errors.Count));
}