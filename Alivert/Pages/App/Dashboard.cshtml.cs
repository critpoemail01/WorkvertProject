using Alivert.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public DashboardModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public int TotalAlerts { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int UniqueSymbols { get; private set; }
    public int TriggersLast7Days { get; private set; }
    public int FailedDeliveriesLast7Days { get; private set; }

    public List<TriggerRow> LatestTriggers { get; private set; } = new();
    public List<DeliveryRow> LatestDeliveries { get; private set; } = new();

    public record TriggerRow(DateTime TriggeredAtUtc, string Symbol, string Message);
    public record DeliveryRow(DateTime CreatedAtUtc, string Symbol, string Channel, string Status, string? ErrorMessage);

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        TotalAlerts = await _db.Alerts.CountAsync(a => a.UserId == userId);
        ActiveAlerts = await _db.Alerts.CountAsync(a => a.UserId == userId && a.IsEnabled);
        UniqueSymbols = await _db.Alerts.Where(a => a.UserId == userId).Select(a => a.Symbol).Distinct().CountAsync();

        var since = DateTime.UtcNow.AddDays(-7);
        TriggersLast7Days = await _db.AlertTriggers
            .AsNoTracking()
            .Where(t => t.Alert!.UserId == userId && t.TriggeredAtUtc >= since)
            .CountAsync();

        FailedDeliveriesLast7Days = await _db.AlertDeliveryLogs
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreatedAtUtc >= since && t.Status == "Failed")
            .CountAsync();

        LatestTriggers = await _db.AlertTriggers
            .AsNoTracking()
            .Where(t => t.Alert!.UserId == userId)
            .OrderByDescending(t => t.TriggeredAtUtc)
            .Take(20)
            .Select(t => new TriggerRow(t.TriggeredAtUtc, t.Alert!.Symbol, t.Message))
            .ToListAsync();

        LatestDeliveries = await _db.AlertDeliveryLogs
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(10)
            .Select(t => new DeliveryRow(t.CreatedAtUtc, t.Symbol, t.Channel, t.Status, t.ErrorMessage))
            .ToListAsync();
    }
}
