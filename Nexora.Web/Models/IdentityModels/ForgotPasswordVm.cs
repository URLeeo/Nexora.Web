using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.IdentityModels;

public class ForgotPasswordVm
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
