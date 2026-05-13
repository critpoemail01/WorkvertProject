using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class UserNotificationSettings
{
    [Key]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public bool EmailEnabled { get; set; } = true;

    [StringLength(500)]
    public string? WebhookUrl { get; set; }

    [StringLength(500)]
    public string? DiscordWebhookUrl { get; set; }

    [StringLength(80)]
    public string? TelegramChatId { get; set; }

    public bool AlertScheduleEnabled { get; set; }

    [StringLength(5)]
    public string? AlertWindowStart { get; set; }

    [StringLength(5)]
    public string? AlertWindowEnd { get; set; }

    [StringLength(80)]
    public string? AlertTimeZone { get; set; }

    [StringLength(32)]
    public string? AlertWindowDays { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
