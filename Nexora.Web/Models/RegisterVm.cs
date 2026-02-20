using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models;

public class RegisterVm
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [Required, MaxLength(200)]
    public string OrganizationName { get; set; } = default!;
}