using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class Category : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public string Name { get; set; } = default!;

        public List<Product> Products { get; set; } = new();
    }
}
