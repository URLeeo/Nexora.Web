namespace Nexora.Web.Models.ImportModels;

public class OrderImportRowVm
{
    public string CustomerFullName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }

    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
}