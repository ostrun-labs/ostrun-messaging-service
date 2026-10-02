using System.ComponentModel.DataAnnotations;

namespace OstrunNotificationService.Infrastructure.Email;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseStartTls { get; set; } = true;

    [Required]
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Ostrun";
}
