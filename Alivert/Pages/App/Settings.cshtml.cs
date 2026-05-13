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

        [Display(Name = "Telegram chat ID")]
        [StringLength(80)]
        public string? TelegramChatId { get; set; }
    }

    public async Task OnGetAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var settings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        Input = new InputModel
        {
            EmailEnabled = settings?.EmailEnabled ?? true,
            WebhookUrl = settings?.WebhookUrl,
            DiscordWebhookUrl = settings?.DiscordWebhookUrl,
            TelegramChatId = settings?.TelegramChatId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TelegramBotConfigured = !string.IsNullOrWhiteSpace(_notificationOptions.CurrentValue.TelegramBotToken);

        ValidateOptionalUrl("Input.WebhookUrl", Input.WebhookUrl);
        ValidateOptionalUrl("Input.DiscordWebhookUrl", Input.DiscordWebhookUrl);

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
        settings.TelegramChatId = Clean(Input.TelegramChatId);

        await _db.SaveChangesAsync();
        StatusMessage = "Notification settings saved.";
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
}
