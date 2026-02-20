using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class Customer : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public string FullName { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }

        public List<Order> Orders { get; set; } = new();
    }
}
