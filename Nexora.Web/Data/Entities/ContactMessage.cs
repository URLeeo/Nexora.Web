using Nexora.Web.Data.Entities;
using Nexora.Web.Data.Models;

namespace Nexora.Web.Data.Entities;

public class ContactMessage : BaseEntity
{
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
