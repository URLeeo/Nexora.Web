using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public enum StockMovementType
    {
        StockIn = 0,
        StockOut = 1,
        Adjustment = 2,
        Sale = 3
    }

    public class StockMovement : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public StockMovementType Type { get; set; }
        public int QuantityChange { get; set; } // +10, -2 etc.
        public string? Note { get; set; }

        public Guid? RelatedOrderId { get; set; }
        public Order? RelatedOrder { get; set; }

    }
}
