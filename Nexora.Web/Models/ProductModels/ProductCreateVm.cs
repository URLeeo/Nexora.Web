using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Nexora.Web.Models.ProductModels;

public class ProductCreateVm
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(80)]
    public string? Sku { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 999999999)]
    public decimal SalePrice { get; set; }

    [Range(0, 999999999)]
    public decimal? CostPrice { get; set; }

    [Range(0, 1000000)]
    public int StockOnHand { get; set; }

    [Range(0, 1000000)]
    public int LowStockThreshold { get; set; } = 5;

    // Optional product image
    public IFormFile? ImageFile { get; set; }

    public bool IsActive { get; set; } = true;
}