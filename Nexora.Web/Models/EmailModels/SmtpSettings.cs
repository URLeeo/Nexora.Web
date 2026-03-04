using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.EmailModels;

public class SmtpSettings
{
    [Required] public string Host { get; set; } = string.Empty;
    [Range(1, 65535)] public int Port { get; set; } = 587;

    [Required] public string User { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;

    [Required, EmailAddress] public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Nexora";

    public bool EnableSsl { get; set; } = true;
}
