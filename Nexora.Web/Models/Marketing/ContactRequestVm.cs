using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.Marketing;

public class ContactRequestVm
{
    // NOTE:
    // - If user is authenticated, FullName/Email are taken from the logged-in account (inputs are hidden in UI).
    // - If user is anonymous, FullName/Email are required.
    [MaxLength(200)]
    public string? FullName { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    [Required, MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    // Honeypot anti-spam field. Real users will never fill this.
    [MaxLength(200)]
    public string? Website { get; set; }
}
