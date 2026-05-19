using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Workvert.Pages.App;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public DashboardModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    public int TotalAlerts { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int UniqueSymbols { get; private set; }
    public int TriggersLast7Days { get; private set; }
    public int FailedDeliveriesLast7Days { get; private set; }
    public bool IsUnlimitedPlan { get; private set; }
    public string PlanName { get; private set; } = "Basic (Free)";
    public string PlanDescription { get; private set; } = "Start with 5 active professional searches.";
    public string PlanCapacityLabel { get; private set; } = "5 active searches";
    public string PlanRemainingLabel { get; private set; } = "5 available";
    public int PlanUsagePercent { get; private set; }
    public DateTime? PlanExpiresOnUtc { get; private set; }
    public int? PlanDaysRemaining { get; private set; }
    public bool HasProfile { get; private set; }
    public int ProfileCompletion { get; private set; }
    public string ProfileHeadline { get; private set; } = "Create your first profile";
    public string ProfileSummary { get; private set; } = "Start with one path and reuse the profile across all paths.";
    public int SavedJobMatches { get; private set; }
    public int SavedServiceOffers { get; private set; }
    public int SavedClientRequests { get; private set; }
    public int SavedProviderMatches { get; private set; }
    public int GeneratedAssets { get; private set; }

    public List<TriggerRow> LatestTriggers { get; private set; } = new();
    public List<DeliveryRow> LatestDeliveries { get; private set; } = new();
    public List<RecommendationRow> LatestRecommendations { get; private set; } = new();
    public List<RequestRow> LatestRequests { get; private set; } = new();

    public record TriggerRow(DateTime TriggeredAtUtc, string Symbol, string Message);
    public record DeliveryRow(DateTime CreatedAtUtc, string Symbol, string Channel, string Status, string? ErrorMessage);
    public record RecommendationRow(string Title, string Type, string Location, int Score, string NextStep);
    public record RequestRow(string Title, string Area, string Status, DateTime CreatedAtUtc);

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);

        TotalAlerts = await _db.Alerts.CountAsync(a => a.UserId == userId);
        ActiveAlerts = limits.ActiveAlerts;
        UniqueSymbols = await _db.Alerts.Where(a => a.UserId == userId).Select(a => a.Symbol).Distinct().CountAsync();

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var latestPaidUnlimited = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        LoadPlanSummary(limits, account, latestPaidUnlimited);
        await LoadWorkvertSummaryAsync(userId);

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

    private async Task LoadWorkvertSummaryAsync(string userId)
    {
        var profile = await _db.ProfessionalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        HasProfile = profile is not null;

        if (profile is not null)
        {
            ProfileHeadline = string.IsNullOrWhiteSpace(profile.Headline)
                ? profile.CurrentProfession
                : profile.Headline;
            ProfileSummary = $"{profile.CurrentProfession} / {FirstNonEmpty(profile.DesiredLocation, "any location")} / {profile.WorkMode}";
            ProfileCompletion = CalculateCompletion(profile);

            SavedJobMatches = await _db.ProfessionalOpportunityMatches
                .AsNoTracking()
                .Where(x => x.ProfessionalProfileId == profile.Id && x.Status == "Suggested")
                .CountAsync();

            SavedServiceOffers = await _db.FreelancerServiceListings
                .AsNoTracking()
                .Where(x => x.ProfessionalProfileId == profile.Id && x.IsActive)
                .CountAsync();

            GeneratedAssets = await _db.GeneratedProfessionalAssets
                .AsNoTracking()
                .Where(x => x.ProfessionalProfileId == profile.Id)
                .CountAsync();

            LatestRecommendations = await _db.ProfessionalOpportunityMatches
                .AsNoTracking()
                .Where(x => x.ProfessionalProfileId == profile.Id && x.Status == "Suggested")
                .OrderByDescending(x => x.CompatibilityScore)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(4)
                .Select(x => new RecommendationRow(
                    x.WorkOpportunity!.Title,
                    x.MatchType,
                    x.WorkOpportunity.Location ?? "Flexible",
                    x.CompatibilityScore,
                    x.SuggestedNextStep ?? "Open the assistant to continue."))
                .ToListAsync();
        }

        SavedClientRequests = await _db.ClientServiceRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .CountAsync();

        SavedProviderMatches = await _db.ClientServiceMatches
            .AsNoTracking()
            .Where(x => x.ClientServiceRequest!.UserId == userId)
            .CountAsync();

        LatestRequests = await _db.ClientServiceRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(3)
            .Select(x => new RequestRow(
                x.Title,
                x.ServiceArea ?? "Service",
                x.Status,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    private static int CalculateCompletion(ProfessionalProfile profile)
    {
        var checks = new[]
        {
            profile.CurrentProfession,
            profile.ExperienceSummary,
            profile.TechnicalSkills,
            profile.Tools,
            profile.Languages,
            profile.DesiredLocation,
            profile.WorkMode,
            profile.EngagementType,
            profile.InterestAreas,
            profile.PortfolioUrl
        };

        return Math.Clamp((int)Math.Round(checks.Count(x => !string.IsNullOrWhiteSpace(x)) / (double)checks.Length * 100), 0, 100);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;
    }

    private void LoadPlanSummary(
        (bool IsUnlimited, int Capacity, int ActiveAlerts, int RemainingSlots) limits,
        UserAccount? account,
        CreditPurchase? latestPaidUnlimited)
    {
        IsUnlimitedPlan = limits.IsUnlimited;
        PlanExpiresOnUtc = account?.UnlimitedUntilUtc;
        PlanDaysRemaining = PlanExpiresOnUtc is not null && PlanExpiresOnUtc.Value > DateTime.UtcNow
            ? Math.Max(0, (int)Math.Ceiling((PlanExpiresOnUtc.Value - DateTime.UtcNow).TotalDays))
            : null;

        if (IsUnlimitedPlan)
        {
            var isAnnual = latestPaidUnlimited?.PlanCode == "unlimited-annual" ||
                           latestPaidUnlimited?.SubscriptionDays >= 365 ||
                           PlanDaysRemaining > 300;

            PlanName = latestPaidUnlimited is null
                ? "Unlimited"
                : isAnnual
                    ? "Unlimited annual"
                    : "Unlimited monthly";
            PlanDescription = PlanDaysRemaining is null
                ? "Unlimited professional searches, recommendations, and reports."
                : $"Unlimited professional searches, recommendations, and reports until {PlanExpiresOnUtc!.Value.ToLocalTime():MMMM d, yyyy}.";
            PlanCapacityLabel = "Unlimited active searches";
            PlanRemainingLabel = PlanDaysRemaining is null
                ? "Active now"
                : $"{PlanDaysRemaining} day{(PlanDaysRemaining == 1 ? "" : "s")} remaining";
            PlanUsagePercent = 100;
            return;
        }

        PlanName = limits.Capacity <= 5 ? "Basic (free)" : "Credit pack";
        PlanDescription = limits.Capacity <= 5
            ? "Free credits to validate recommendations before paying."
            : "Active capacity for professional searches, alerts, and reports.";
        PlanCapacityLabel = $"{limits.Capacity} active search{(limits.Capacity == 1 ? "" : "es")}";
        PlanRemainingLabel = $"{limits.RemainingSlots} available";
        PlanUsagePercent = limits.Capacity <= 0
            ? 100
            : Math.Clamp((int)Math.Round((double)limits.ActiveAlerts / limits.Capacity * 100), 0, 100);
    }

}
