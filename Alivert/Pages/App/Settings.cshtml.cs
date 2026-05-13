using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Alivert.Pages.App;

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

        [Display(Name = "Notification time zone")]
        [StringLength(80)]
        public string AlertTimeZone { get; set; } = TimeZoneCatalog.DefaultTimeZoneId;
    }

    public async Task OnGetAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);
        EmailTransportConfigured = IsEmailTransportConfigured();

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
            AlertTimeZone = TimeZoneCatalog.Normalize(settings?.AlertTimeZone)
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);
        EmailTransportConfigured = IsEmailTransportConfigured();

        ValidateOptionalUrl("Input.WebhookUrl", Input.WebhookUrl);
        ValidateOptionalUrl("Input.DiscordWebhookUrl", Input.DiscordWebhookUrl);
        ValidateOptionalUrl("Input.SlackWebhookUrl", Input.SlackWebhookUrl);
        ValidateOptionalUrl("Input.TeamsWebhookUrl", Input.TeamsWebhookUrl);

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
        settings.AlertTimeZone = TimeZoneCatalog.Normalize(Input.AlertTimeZone);

        await _db.SaveChangesAsync();
        StatusMessage = "Marketing channel settings saved.";
        return RedirectToPage();
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

    private static string? Clean(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private bool IsEmailTransportConfigured()
    {
        var email = _notificationOptions.CurrentValue.Email;
        return !string.IsNullOrWhiteSpace(email.FromEmail) &&
            !string.IsNullOrWhiteSpace(email.Password) &&
            !string.IsNullOrWhiteSpace(email.SmtpServer) &&
            email.SmtpPort > 0;
    }
}
