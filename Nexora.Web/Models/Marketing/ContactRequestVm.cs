using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.Marketing;

public class ContactRequestVm
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}
