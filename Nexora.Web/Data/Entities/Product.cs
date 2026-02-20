using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class Product : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        public string Name { get; set; } = default!;
        public string? Sku { get; set; }

        public decimal SalePrice { get; set; }
        public decimal? CostPrice { get; set; }

        public int StockOnHand { get; set; }
        public int LowStockThreshold { get; set; } = 5;

        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;


        public List<OrderItem> OrderItems { get; set; } = new();
        public List<StockMovement> StockMovements { get; set; } = new();
    }
}
