using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.IdentityModels;

public class AccountSettingsVm
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    // Change password (optional)
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password), MinLength(6), MaxLength(100)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string? ConfirmNewPassword { get; set; }
}
