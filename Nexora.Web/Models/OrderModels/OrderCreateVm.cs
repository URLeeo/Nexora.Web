using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.OrderModels;

public class OrderCreateVm
{
    public Guid? CustomerId { get; set; }

    [Range(0, 999999999)]
    public decimal DiscountAmount { get; set; }

    [Range(0, 999999999)]
    public decimal TaxAmount { get; set; }

    public List<OrderCreateItemVm> Items { get; set; } = new();
}

public class OrderCreateItemVm
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; }

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }
}