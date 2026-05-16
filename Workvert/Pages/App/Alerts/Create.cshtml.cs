using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Workvert.Pages.App.Alerts;

[Authorize]
public class CreateAlertModel : PageModel
{
    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email",
        "Webhook",
        "Slack",
        "Discord",
        "Teams",
        "Telegram"
    };

    private static readonly HashSet<string> SupportedLocationScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "World",
        "Country",
        "City",
        "Custom"
    };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public CreateAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList RuleTypeOptions { get; private set; } = default!;
    public SelectList TimeframeOptions { get; private set; } = default!;
    public SelectList MarketTypeOptions { get; private set; } = default!;

    public bool IsUnlimited { get; private set; }
    public int RemainingSlots { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int Capacity { get; private set; }

    public class InputModel
    {
        [Display(Name = "Product URL")]
        [Required, StringLength(180, ErrorMessage = "Use 180 characters or fewer.")]
        public string Symbol { get; set; } = "https://www.amazon.es/dp/example";

        [Display(Name = "Monitoring type")]
        [Required]
        public MarketType MarketType { get; set; } = MarketType.Crypto;

        [Display(Name = "Rule")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceBelow;

        [Display(Name = "Target price EUR")]
        [Required, Range(0.01, 999999)]
        public decimal Threshold { get; set; } = 100m;

        [Display(Name = "Frequency")]
        [Required, StringLength(8)]
        public string Timeframe { get; set; } = "6h";

        [Display(Name = "Margin below target (%)")]
        [Range(0.01, 100)]
        public decimal ZonePercent { get; set; } = 5.0m;

        [Range(2, 100)]
        public int RsiPeriod { get; set; } = 14;

        [Range(1, 200)]
        public int FastEmaPeriod { get; set; } = 3;

        [Range(2, 250)]
        public int SlowEmaPeriod { get; set; } = 5;

        [Display(Name = "Cooldown between alerts (min)")]
        [Range(1, 1440)]
        public int CooldownMinutes { get; set; } = 240;

        [Display(Name = "Active")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Channel")]
        [StringLength(32)]
        public string Channel { get; set; } = "Email";

        [Display(Name = "Search area")]
        [Required, StringLength(16)]
        public string LocationScope { get; set; } = ProductWatchMetadata.Default.LocationScope;

        [Display(Name = "Country")]
        [StringLength(80)]
        public string Country { get; set; } = ProductWatchMetadata.Default.Country;

        [Display(Name = "City")]
        [StringLength(80)]
        public string City { get; set; } = ProductWatchMetadata.Default.City;

        [Display(Name = "Category")]
        [Required, StringLength(120)]
        public string Category { get; set; } = ProductWatchMetadata.Default.Category;

        [Display(Name = "Latitude")]
        [StringLength(32)]
        public string? Latitude { get; set; } = ProductWatchMetadata.Default.Latitude?.ToString(CultureInfo.InvariantCulture);

        [Display(Name = "Longitude")]
        [StringLength(32)]
        public string? Longitude { get; set; } = ProductWatchMetadata.Default.Longitude?.ToString(CultureInfo.InvariantCulture);

        [Display(Name = "Radius")]
        [Range(1, 1000)]
        public int RadiusKm { get; set; } = ProductWatchMetadata.Default.RadiusKm ?? 25;

        [Display(Name = "Trusted stores")]
        [StringLength(800)]
        public string TrustedStores { get; set; } = ProductWatchMetadata.Default.TrustedStores;

        [Display(Name = "Resale sites")]
        [StringLength(800)]
        public string SecondHandSites { get; set; } = ProductWatchMetadata.Default.SecondHandSites;

        [Display(Name = "Report email")]
        [StringLength(160)]
        public string? ReportEmail { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(MarketType? marketType = null, string? symbol = null, AlertRuleType? ruleType = null, string? timeframe = null, decimal? threshold = null)
    {
        await LoadManualFormAsync(marketType, symbol, ruleType, timeframe, threshold);
        return Page();
    }

    public async Task LoadManualFormAsync(MarketType? marketType = null, string? symbol = null, AlertRuleType? ruleType = null, string? timeframe = null, decimal? threshold = null)
    {
        LoadSelectLists();

        if (marketType is not null)
            Input.MarketType = marketType.Value;

        if (!string.IsNullOrWhiteSpace(symbol))
            Input.Symbol = NormalizeSource(symbol);

        if (ruleType is not null)
            Input.RuleType = ruleType.Value;

        if (!string.IsNullOrWhiteSpace(timeframe))
            Input.Timeframe = NormalizeTimeframe(timeframe);

        if (threshold is not null)
            Input.Threshold = threshold.Value;

        NormalizeRuleInputs();
        await LoadAccountLimitsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadSelectLists();
        await LoadAccountLimitsAsync();

        if (!ModelState.IsValid)
            return Page();

        ValidateProductInputs();
        if (!ModelState.IsValid)
            return Page();

        if (Input.IsEnabled && !IsUnlimited && RemainingSlots <= 0)
        {
            ModelState.AddModelError(string.Empty, "No active credits available. Pause another product or upgrade the plan before enabling more alerts.");
            return Page();
        }

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var sourceUrl = NormalizeSource(Input.Symbol);
        var timeframe = NormalizeTimeframe(Input.Timeframe);
        var channel = NormalizeChannel(Input.Channel);
        var metadata = BuildMetadata().Serialize();

        var exists = await _db.Alerts.AnyAsync(a =>
            a.UserId == userId &&
            a.Symbol == sourceUrl &&
            a.MarketType == Input.MarketType &&
            a.RuleType == Input.RuleType &&
            a.Threshold == Input.Threshold &&
            a.Timeframe == timeframe &&
            a.ZonePercent == Input.ZonePercent &&
            a.Channel == channel &&
            a.AudienceList == metadata);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "An identical alert already exists for this product.");
            return Page();
        }

        _db.Alerts.Add(new Alert
        {
            UserId = userId,
            Symbol = sourceUrl,
            MarketType = Input.MarketType,
            RuleType = Input.RuleType,
            Threshold = Input.Threshold,
            Timeframe = timeframe,
            ZonePercent = Input.ZonePercent,
            RsiPeriod = 14,
            FastEmaPeriod = 3,
            SlowEmaPeriod = 5,
            CooldownMinutes = Input.RuleType == AlertRuleType.DailyOpportunityReport ? 1440 : Input.CooldownMinutes,
            IsEnabled = Input.IsEnabled,
            Channel = channel,
            AudienceList = metadata
        });

        await _db.SaveChangesAsync();
        return RedirectToPage("/App/Alerts/Index");
    }

    private async Task LoadAccountLimitsAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimited = limits.IsUnlimited;
        RemainingSlots = limits.RemainingSlots;
        ActiveAlerts = limits.ActiveAlerts;
        Capacity = limits.Capacity;
    }

    private void LoadSelectLists()
    {
        RuleTypeOptions = new SelectList(new[]
        {
            new { Value = AlertRuleType.PriceBelow.ToString(), Text = AlertRuleType.PriceBelow.DisplayName() },
            new { Value = AlertRuleType.PriceBelowMargin.ToString(), Text = AlertRuleType.PriceBelowMargin.DisplayName() },
            new { Value = AlertRuleType.PriceZone.ToString(), Text = AlertRuleType.PriceZone.DisplayName() },
            new { Value = AlertRuleType.DailyOpportunityReport.ToString(), Text = AlertRuleType.DailyOpportunityReport.DisplayName() }
        }, "Value", "Text");

        TimeframeOptions = new SelectList(new[]
        {
            new { Value = "1h", Text = "Hourly" },
            new { Value = "6h", Text = "Every 6 hours" },
            new { Value = "12h", Text = "Every 12 hours" },
            new { Value = "1d", Text = "Daily" }
        }, "Value", "Text");

        MarketTypeOptions = new SelectList(new[]
        {
            new { Value = MarketType.Crypto.ToString(), Text = "Trusted-store alerts" },
            new { Value = MarketType.Traditional.ToString(), Text = "Resale opportunity report" }
        }, "Value", "Text");
    }

    private void ValidateProductInputs()
    {
        NormalizeRuleInputs();

        if (!Uri.TryCreate(Input.Symbol?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ModelState.AddModelError("Input.Symbol", "Enter a valid product URL, for example https://www.amazon.es/dp/...");
        }

        if (!SupportedChannels.Contains(Input.Channel ?? string.Empty))
            ModelState.AddModelError("Input.Channel", "Choose Email, Webhook, Slack, Discord, Teams, or Telegram.");

        if (!string.IsNullOrWhiteSpace(Input.ReportEmail) && !Input.ReportEmail.Contains('@', StringComparison.Ordinal))
            ModelState.AddModelError("Input.ReportEmail", "Enter a valid email address for reports.");

        if (Input.LocationScope is "Country" or "City" or "Custom" && string.IsNullOrWhiteSpace(Input.Country))
            ModelState.AddModelError("Input.Country", "Choose the country for this search area.");

        if (Input.LocationScope is "City" or "Custom" && string.IsNullOrWhiteSpace(Input.City))
            ModelState.AddModelError("Input.City", "Choose the city for this search area.");

        if (Input.LocationScope == "Custom" && (Input.RadiusKm < 1 || Input.RadiusKm > 1000))
            ModelState.AddModelError("Input.RadiusKm", "Use a radius between 1 and 1000 km.");
    }

    private void NormalizeRuleInputs()
    {
        Input.Timeframe = NormalizeTimeframe(Input.Timeframe);
        Input.Symbol = NormalizeSource(Input.Symbol);
        Input.LocationScope = NormalizeLocationScope(Input.LocationScope);
        Input.Country = Input.LocationScope == "World"
            ? "World"
            : NormalizeText(Input.Country, ProductWatchMetadata.Default.Country);
        Input.City = Input.LocationScope is "City" or "Custom"
            ? NormalizeText(Input.City, ProductWatchMetadata.Default.City)
            : string.Empty;
        Input.Category = NormalizeText(Input.Category, ProductWatchMetadata.Default.Category);
        Input.TrustedStores = NormalizeText(Input.TrustedStores, ProductWatchMetadata.Default.TrustedStores);
        Input.SecondHandSites = NormalizeText(Input.SecondHandSites, ProductWatchMetadata.Default.SecondHandSites);
        Input.RadiusKm = Math.Clamp(Input.RadiusKm, 1, 1000);

        if (Input.RuleType == AlertRuleType.DailyOpportunityReport)
        {
            Input.Timeframe = "1d";
            Input.CooldownMinutes = 1440;
            if (Input.Threshold <= 0)
                Input.Threshold = 20m;
        }
    }

    private ProductWatchMetadata BuildMetadata()
    {
        var scope = NormalizeLocationScope(Input.LocationScope);
        var latitude = scope == "Custom" ? ParseCoordinate(Input.Latitude, -90m, 90m) : null;
        var longitude = scope == "Custom" ? ParseCoordinate(Input.Longitude, -180m, 180m) : null;

        return new ProductWatchMetadata(
            scope == "World" ? "World" : NormalizeText(Input.Country, ProductWatchMetadata.Default.Country),
            scope is "City" or "Custom" ? NormalizeText(Input.City, string.Empty) : string.Empty,
            NormalizeText(Input.Category, ProductWatchMetadata.Default.Category),
            NormalizeText(Input.TrustedStores, ProductWatchMetadata.Default.TrustedStores),
            NormalizeText(Input.SecondHandSites, ProductWatchMetadata.Default.SecondHandSites),
            NormalizeText(Input.ReportEmail, string.Empty),
            scope,
            latitude,
            longitude,
            scope == "Custom" ? Math.Clamp(Input.RadiusKm, 1, 1000) : null);
    }

    private static string NormalizeTimeframe(string? timeframe)
    {
        return timeframe?.Trim().ToLowerInvariant() switch
        {
            "1h" => "1h",
            "6h" => "6h",
            "12h" => "12h",
            "1d" => "1d",
            _ => "6h"
        };
    }

    private static string NormalizeChannel(string? channel)
    {
        var value = NormalizeText(channel, "Email");
        return SupportedChannels.Contains(value) ? value : "Email";
    }

    private static string NormalizeLocationScope(string? scope)
    {
        var value = NormalizeText(scope, ProductWatchMetadata.Default.LocationScope);
        return SupportedLocationScopes.TryGetValue(value, out var supported) ? supported : ProductWatchMetadata.Default.LocationScope;
    }

    private static decimal? ParseCoordinate(string? value, decimal min, decimal max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return null;

        return parsed < min || parsed > max ? null : parsed;
    }

    private static string NormalizeSource(string? source) => (source ?? string.Empty).Trim();

    private static string NormalizeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
