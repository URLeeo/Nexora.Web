using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class Organization : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public List<AppUser> Users { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<StockMovement> StockMovements { get; set; } = new();
        public List<Subscription> Subscriptions { get; set; } = new();
    }

}
