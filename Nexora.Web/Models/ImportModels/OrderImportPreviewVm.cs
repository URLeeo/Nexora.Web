namespace Nexora.Web.Models.ImportModels;

public class OrderImportPreviewVm
{
    public string CustomerFullName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<OrderImportItemPreviewVm> Items { get; set; } = new();
}

public class OrderImportItemPreviewVm
{
    // resolved by preview
    public Guid ProductId { get; set; }

    // from file
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }

    // resolved display
    public string? ResolvedProductSku { get; set; }
    public string? ResolvedProductName { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public List<string> Errors { get; set; } = new();
}