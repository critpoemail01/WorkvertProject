using Alivert.Data;
using Alivert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App.Alerts;

[Authorize]
public class AlertsIndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public AlertsIndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Alert> Alerts { get; private set; } = new();
    public List<AlertGroup> AlertGroups { get; private set; } = new();
    public UserNotificationSettings? ScheduleSettings { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public static readonly string[] Timeframes = ["5m", "15m", "1h", "4h", "1d", "1wk", "1mo"];

    public record AlertGroup(MarketType MarketType, string Symbol, List<Alert> Alerts);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostScheduleAsync(string startTime, string endTime, string timeZone, string[] days)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        if (!TimeSpan.TryParse(startTime, out _) || !TimeSpan.TryParse(endTime, out _))
        {
            StatusMessage = "Use valid start and end times for the schedule.";
            return RedirectToPage();
        }

        var settings = await _db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            _db.UserNotificationSettings.Add(settings);
        }

        settings.AlertScheduleEnabled = true;
        settings.AlertWindowStart = NormalizeTime(startTime);
        settings.AlertWindowEnd = NormalizeTime(endTime);
        settings.AlertTimeZone = string.IsNullOrWhiteSpace(timeZone) ? "Europe/Lisbon" : timeZone.Trim();
        settings.AlertWindowDays = NormalizeDays(days);
        await _db.SaveChangesAsync();

        StatusMessage = "Alert schedule saved. Notifications outside this window will be skipped.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveScheduleAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var settings = await _db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is not null)
        {
            settings.AlertScheduleEnabled = false;
            settings.AlertWindowStart = null;
            settings.AlertWindowEnd = null;
            settings.AlertWindowDays = null;
            await _db.SaveChangesAsync();
        }

        StatusMessage = "Alert schedule removed. Notifications are back to 24/7 delivery.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        Alerts = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsEnabled)
            .ThenBy(a => a.Symbol)
            .ToListAsync();

        AlertGroups = Alerts
            .GroupBy(a => new { a.MarketType, a.Symbol })
            .Select(g => new AlertGroup(g.Key.MarketType, g.Key.Symbol, g.ToList()))
            .OrderBy(g => g.MarketType)
            .ThenBy(g => g.Symbol)
            .ToList();

        ScheduleSettings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private static string NormalizeTime(string value)
    {
        return TimeSpan.TryParse(value, out var parsed)
            ? parsed.ToString(@"hh\:mm")
            : "00:00";
    }

    private static string NormalizeDays(string[] days)
    {
        var allowed = new HashSet<string>(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], StringComparer.OrdinalIgnoreCase);
        var selected = days.Where(allowed.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return selected.Length == 0 ? "Mon,Tue,Wed,Thu,Fri,Sat,Sun" : string.Join(",", selected);
    }
}
