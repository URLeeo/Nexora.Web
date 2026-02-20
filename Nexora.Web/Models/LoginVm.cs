using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; }
}