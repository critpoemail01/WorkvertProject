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

    [StringLength(500)]
    public string? SlackWebhookUrl { get; set; }

    [StringLength(500)]
    public string? TeamsWebhookUrl { get; set; }

    [StringLength(80)]
    public string? TelegramChatId { get; set; }

    public bool LinkedInAuthorized { get; set; }

    [StringLength(120)]
    public string? LinkedInOrganizationId { get; set; }

    [StringLength(160)]
    public string? LinkedInOrganizationName { get; set; }

    [StringLength(240)]
    public string? LinkedInScopes { get; set; }

    public DateTime? LinkedInAuthorizedAtUtc { get; set; }

    public bool InstagramAuthorized { get; set; }

    [StringLength(120)]
    public string? InstagramBusinessAccountId { get; set; }

    [StringLength(160)]
    public string? InstagramBusinessAccountName { get; set; }

    [StringLength(240)]
    public string? InstagramScopes { get; set; }

    public DateTime? InstagramAuthorizedAtUtc { get; set; }

    public bool FacebookAuthorized { get; set; }

    [StringLength(120)]
    public string? FacebookPageId { get; set; }

    [StringLength(160)]
    public string? FacebookPageName { get; set; }

    [StringLength(240)]
    public string? FacebookScopes { get; set; }

    public DateTime? FacebookAuthorizedAtUtc { get; set; }

    public bool GoogleBusinessAuthorized { get; set; }

    [StringLength(160)]
    public string? GoogleBusinessProfileName { get; set; }

    public DateTime? GoogleBusinessAuthorizedAtUtc { get; set; }

    [StringLength(80)]
    public string? EmailProvider { get; set; }

    public bool WhatsAppAuthorized { get; set; }

    [StringLength(120)]
    public string? WhatsAppProviderName { get; set; }

    public DateTime? WhatsAppAuthorizedAtUtc { get; set; }

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
