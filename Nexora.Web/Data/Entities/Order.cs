using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public enum OrderStatus
    {
        Draft = 0,
        Paid = 1,
        Cancelled = 2
    }

    public class Order : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string OrderNo { get; set; } = default!;
        public OrderStatus Status { get; set; } = OrderStatus.Paid;

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
