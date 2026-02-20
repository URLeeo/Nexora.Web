using Microsoft.AspNetCore.Identity;
namespace Nexora.Web.Data.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public string FullName { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
