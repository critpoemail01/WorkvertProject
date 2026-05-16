using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Workvert.Pages.App;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IOptionsMonitor<NotificationOptions> _notificationOptions;

    public SettingsModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IOptionsMonitor<NotificationOptions> notificationOptions)
    {
        _db = db;
        _userManager = userManager;
        _notificationOptions = notificationOptions;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool TelegramBotConfigured { get; private set; }
    public bool EmailTransportConfigured { get; private set; }
    public int EmailSenderProfileCount { get; private set; }
    public IReadOnlyList<ScheduleTimeZoneChoice> NotificationTimeZones { get; private set; } = TimeZoneCatalog.GetScheduleChoices(DateTime.UtcNow);

    public class InputModel
    {
        [Display(Name = "Email enabled")]
        public bool EmailEnabled { get; set; } = true;

        [Display(Name = "Webhook URL")]
        [StringLength(500)]
        public string? WebhookUrl { get; set; }

        [Display(Name = "Discord webhook URL")]
        [StringLength(500)]
        public string? DiscordWebhookUrl { get; set; }

        [Display(Name = "Slack webhook URL")]
        [StringLength(500)]
        public string? SlackWebhookUrl { get; set; }

        [Display(Name = "Microsoft Teams webhook URL")]
        [StringLength(500)]
        public string? TeamsWebhookUrl { get; set; }

        [Display(Name = "Telegram chat ID")]
        [StringLength(80)]
        public string? TelegramChatId { get; set; }

        [Display(Name = "LinkedIn Company Page authorized")]
        public bool LinkedInAuthorized { get; set; }

        [Display(Name = "LinkedIn organization ID")]
        [StringLength(120)]
        public string? LinkedInOrganizationId { get; set; }

        [Display(Name = "LinkedIn organization name")]
        [StringLength(160)]
        public string? LinkedInOrganizationName { get; set; }

        [Display(Name = "LinkedIn OAuth scopes")]
        [StringLength(240)]
        public string? LinkedInScopes { get; set; } = "w_organization_social";

        [Display(Name = "Instagram Business authorized")]
        public bool InstagramAuthorized { get; set; }

        [Display(Name = "Instagram business account ID")]
        [StringLength(120)]
        public string? InstagramBusinessAccountId { get; set; }

        [Display(Name = "Instagram business account name")]
        [StringLength(160)]
        public string? InstagramBusinessAccountName { get; set; }

        [Display(Name = "Instagram OAuth scopes")]
        [StringLength(240)]
        public string? InstagramScopes { get; set; } = "instagram_basic,pages_show_list,pages_read_engagement,instagram_content_publish";

        [Display(Name = "Facebook Page authorized")]
        public bool FacebookAuthorized { get; set; }

        [Display(Name = "Facebook page ID")]
        [StringLength(120)]
        public string? FacebookPageId { get; set; }

        [Display(Name = "Facebook page name")]
        [StringLength(160)]
        public string? FacebookPageName { get; set; }

        [Display(Name = "Facebook OAuth scopes")]
        [StringLength(240)]
        public string? FacebookScopes { get; set; } = "pages_manage_posts,pages_read_engagement";

        [Display(Name = "Google Business Profile authorized")]
        public bool GoogleBusinessAuthorized { get; set; }

        [Display(Name = "Google Business Profile name")]
        [StringLength(160)]
        public string? GoogleBusinessProfileName { get; set; }

        [Display(Name = "Email provider")]
        [StringLength(80)]
        public string? EmailProvider { get; set; } = "Brevo";

        [Display(Name = "WhatsApp provider authorized")]
        public bool WhatsAppAuthorized { get; set; }

        [Display(Name = "WhatsApp provider name")]
        [StringLength(120)]
        public string? WhatsAppProviderName { get; set; }

        [Display(Name = "Google Analytics measurement ID")]
        [StringLength(40)]
        public string? GoogleAnalyticsMeasurementId { get; set; }

        [Display(Name = "Meta Pixel ID")]
        [StringLength(80)]
        public string? MetaPixelId { get; set; }

        [Display(Name = "Agency name")]
        [StringLength(160)]
        public string? AgencyName { get; set; }

        [Display(Name = "Client workspace label")]
        [StringLength(80)]
        public string? AgencyWorkspaceName { get; set; }

        [Display(Name = "Agency brand color")]
        [StringLength(16)]
        public string? AgencyBrandColor { get; set; }

        [Display(Name = "Report footer")]
        [StringLength(260)]
        public string? AgencyReportFooter { get; set; }

        [Display(Name = "Permission mode")]
        [StringLength(40)]
        public string? AgencyPermissionMode { get; set; } = "OwnerOnly";

        [Display(Name = "Notification time zone")]
        [StringLength(80)]
        public string AlertTimeZone { get; set; } = TimeZoneCatalog.DefaultTimeZoneId;
    }

    public async Task OnGetAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);
        EmailTransportConfigured = IsEmailTransportConfigured();
        EmailSenderProfileCount = CountEmailSenderProfiles();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var settings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        Input = new InputModel
        {
            EmailEnabled = settings?.EmailEnabled ?? true,
            WebhookUrl = settings?.WebhookUrl,
            DiscordWebhookUrl = settings?.DiscordWebhookUrl,
            SlackWebhookUrl = settings?.SlackWebhookUrl,
            TeamsWebhookUrl = settings?.TeamsWebhookUrl,
            TelegramChatId = settings?.TelegramChatId,
            LinkedInAuthorized = settings?.LinkedInAuthorized ?? false,
            LinkedInOrganizationId = settings?.LinkedInOrganizationId,
            LinkedInOrganizationName = settings?.LinkedInOrganizationName,
            LinkedInScopes = settings?.LinkedInScopes ?? "w_organization_social",
            InstagramAuthorized = settings?.InstagramAuthorized ?? false,
            InstagramBusinessAccountId = settings?.InstagramBusinessAccountId,
            InstagramBusinessAccountName = settings?.InstagramBusinessAccountName,
            InstagramScopes = settings?.InstagramScopes ?? "instagram_basic,pages_show_list,pages_read_engagement,instagram_content_publish",
            FacebookAuthorized = settings?.FacebookAuthorized ?? false,
            FacebookPageId = settings?.FacebookPageId,
            FacebookPageName = settings?.FacebookPageName,
            FacebookScopes = settings?.FacebookScopes ?? "pages_manage_posts,pages_read_engagement",
            GoogleBusinessAuthorized = settings?.GoogleBusinessAuthorized ?? false,
            GoogleBusinessProfileName = settings?.GoogleBusinessProfileName,
            EmailProvider = settings?.EmailProvider ?? "Brevo",
            WhatsAppAuthorized = settings?.WhatsAppAuthorized ?? false,
            WhatsAppProviderName = settings?.WhatsAppProviderName,
            GoogleAnalyticsMeasurementId = settings?.GoogleAnalyticsMeasurementId,
            MetaPixelId = settings?.MetaPixelId,
            AgencyName = settings?.AgencyName,
            AgencyWorkspaceName = settings?.AgencyWorkspaceName,
            AgencyBrandColor = settings?.AgencyBrandColor,
            AgencyReportFooter = settings?.AgencyReportFooter,
            AgencyPermissionMode = settings?.AgencyPermissionMode ?? "OwnerOnly",
            AlertTimeZone = TimeZoneCatalog.Normalize(settings?.AlertTimeZone)
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);
        EmailTransportConfigured = IsEmailTransportConfigured();
        EmailSenderProfileCount = CountEmailSenderProfiles();

        ValidateOptionalUrl("Input.WebhookUrl", Input.WebhookUrl);
        ValidateOptionalUrl("Input.DiscordWebhookUrl", Input.DiscordWebhookUrl);
        ValidateOptionalUrl("Input.SlackWebhookUrl", Input.SlackWebhookUrl);
        ValidateOptionalUrl("Input.TeamsWebhookUrl", Input.TeamsWebhookUrl);
        ValidateAuthorizedAccount(Input.LinkedInAuthorized, "Input.LinkedInOrganizationId", Input.LinkedInOrganizationId, "Add the authorized LinkedIn organization ID.");
        ValidateAuthorizedAccount(Input.InstagramAuthorized, "Input.InstagramBusinessAccountId", Input.InstagramBusinessAccountId, "Add the authorized Instagram Business account ID.");
        ValidateAuthorizedAccount(Input.FacebookAuthorized, "Input.FacebookPageId", Input.FacebookPageId, "Add the authorized Facebook Page ID.");
        ValidateAuthorizedAccount(Input.GoogleBusinessAuthorized, "Input.GoogleBusinessProfileName", Input.GoogleBusinessProfileName, "Add the authorized Google Business Profile name.");
        ValidateAuthorizedAccount(Input.WhatsAppAuthorized, "Input.WhatsAppProviderName", Input.WhatsAppProviderName, "Add the authorized WhatsApp provider name.");
        ValidateTrackingId("Input.GoogleAnalyticsMeasurementId", Input.GoogleAnalyticsMeasurementId, "Use a valid GA4 measurement ID, for example G-ABC123XYZ.");
        ValidateTrackingId("Input.MetaPixelId", Input.MetaPixelId, "Use a valid Meta Pixel ID.");
        ValidateOptionalColor("Input.AgencyBrandColor", Input.AgencyBrandColor);

        if (!ModelState.IsValid)
            return Page();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var settings = await _db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            _db.UserNotificationSettings.Add(settings);
        }

        settings.EmailEnabled = Input.EmailEnabled;
        settings.WebhookUrl = Clean(Input.WebhookUrl);
        settings.DiscordWebhookUrl = Clean(Input.DiscordWebhookUrl);
        settings.SlackWebhookUrl = Clean(Input.SlackWebhookUrl);
        settings.TeamsWebhookUrl = Clean(Input.TeamsWebhookUrl);
        settings.TelegramChatId = Clean(Input.TelegramChatId);
        ApplyOfficialIntegrations(settings);
        ApplyAgencyWhiteLabel(settings);
        settings.AlertTimeZone = TimeZoneCatalog.Normalize(Input.AlertTimeZone);

        await _db.SaveChangesAsync();
        StatusMessage = "Notification channel settings saved.";
        return RedirectToPage();
    }

    private void ApplyOfficialIntegrations(UserNotificationSettings settings)
    {
        var now = DateTime.UtcNow;

        settings.LinkedInAuthorized = Input.LinkedInAuthorized;
        settings.LinkedInOrganizationId = Clean(Input.LinkedInOrganizationId);
        settings.LinkedInOrganizationName = Clean(Input.LinkedInOrganizationName);
        settings.LinkedInScopes = Clean(Input.LinkedInScopes);
        settings.LinkedInAuthorizedAtUtc = Input.LinkedInAuthorized
            ? settings.LinkedInAuthorizedAtUtc ?? now
            : null;

        settings.InstagramAuthorized = Input.InstagramAuthorized;
        settings.InstagramBusinessAccountId = Clean(Input.InstagramBusinessAccountId);
        settings.InstagramBusinessAccountName = Clean(Input.InstagramBusinessAccountName);
        settings.InstagramScopes = Clean(Input.InstagramScopes);
        settings.InstagramAuthorizedAtUtc = Input.InstagramAuthorized
            ? settings.InstagramAuthorizedAtUtc ?? now
            : null;

        settings.FacebookAuthorized = Input.FacebookAuthorized;
        settings.FacebookPageId = Clean(Input.FacebookPageId);
        settings.FacebookPageName = Clean(Input.FacebookPageName);
        settings.FacebookScopes = Clean(Input.FacebookScopes);
        settings.FacebookAuthorizedAtUtc = Input.FacebookAuthorized
            ? settings.FacebookAuthorizedAtUtc ?? now
            : null;

        settings.GoogleBusinessAuthorized = Input.GoogleBusinessAuthorized;
        settings.GoogleBusinessProfileName = Clean(Input.GoogleBusinessProfileName);
        settings.GoogleBusinessAuthorizedAtUtc = Input.GoogleBusinessAuthorized
            ? settings.GoogleBusinessAuthorizedAtUtc ?? now
            : null;

        settings.EmailProvider = Clean(Input.EmailProvider);
        settings.WhatsAppAuthorized = Input.WhatsAppAuthorized;
        settings.WhatsAppProviderName = Clean(Input.WhatsAppProviderName);
        settings.WhatsAppAuthorizedAtUtc = Input.WhatsAppAuthorized
            ? settings.WhatsAppAuthorizedAtUtc ?? now
            : null;
        settings.GoogleAnalyticsMeasurementId = Clean(Input.GoogleAnalyticsMeasurementId);
        settings.MetaPixelId = Clean(Input.MetaPixelId);
    }

    private void ApplyAgencyWhiteLabel(UserNotificationSettings settings)
    {
        settings.AgencyName = Clean(Input.AgencyName);
        settings.AgencyWorkspaceName = Clean(Input.AgencyWorkspaceName);
        settings.AgencyBrandColor = Clean(Input.AgencyBrandColor);
        settings.AgencyReportFooter = Clean(Input.AgencyReportFooter);
        settings.AgencyPermissionMode = NormalizePermissionMode(Input.AgencyPermissionMode);
    }

    private void ValidateOptionalUrl(string fieldName, string? value)
    {
        var cleaned = Clean(value);
        if (cleaned is null)
            return;

        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            ModelState.AddModelError(fieldName, "Use a valid http or https URL.");
        }
    }

    private void ValidateAuthorizedAccount(bool enabled, string fieldName, string? value, string message)
    {
        if (enabled && string.IsNullOrWhiteSpace(value))
            ModelState.AddModelError(fieldName, message);
    }

    private void ValidateTrackingId(string fieldName, string? value, string message)
    {
        var cleaned = Clean(value);
        if (cleaned is null)
            return;

        if (!Regex.IsMatch(cleaned, "^[A-Za-z0-9_-]{4,40}$"))
            ModelState.AddModelError(fieldName, message);
    }

    private void ValidateOptionalColor(string fieldName, string? value)
    {
        var cleaned = Clean(value);
        if (cleaned is null)
            return;

        if (!Regex.IsMatch(cleaned, "^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$"))
            ModelState.AddModelError(fieldName, "Use a valid hex color, for example #14b8a6.");
    }

    private static string? Clean(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string NormalizePermissionMode(string? value)
    {
        var cleaned = Clean(value);
        return cleaned is "TeamReview" or "ClientApproval" ? cleaned : "OwnerOnly";
    }

    private bool IsEmailTransportConfigured()
    {
        return CountEmailSenderProfiles() > 0;
    }

    private int CountEmailSenderProfiles()
    {
        return EmailSenderPool.GetConfiguredSenders(_notificationOptions.CurrentValue.Email).Count;
    }
}
