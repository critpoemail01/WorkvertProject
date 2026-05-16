using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dealvert.Pages.App.Alerts;

[Authorize]
public class EditAlertModel : PageModel
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

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public EditAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
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

    public class InputModel
    {
        public int Id { get; set; }

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

        [Display(Name = "Country")]
        [Required, StringLength(80)]
        public string Country { get; set; } = ProductWatchMetadata.Default.Country;

        [Display(Name = "City")]
        [StringLength(80)]
        public string City { get; set; } = ProductWatchMetadata.Default.City;

        [Display(Name = "Category")]
        [Required, StringLength(120)]
        public string Category { get; set; } = ProductWatchMetadata.Default.Category;

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        LoadSelectLists();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        var metadata = ProductWatchMetadata.Parse(alert.AudienceList);
        Input = new InputModel
        {
            Id = alert.Id,
            Symbol = alert.Symbol,
            MarketType = alert.MarketType,
            RuleType = alert.RuleType,
            Threshold = alert.Threshold,
            Timeframe = alert.Timeframe,
            ZonePercent = alert.ZonePercent,
            CooldownMinutes = alert.CooldownMinutes,
            IsEnabled = alert.IsEnabled,
            Channel = alert.Channel,
            Country = metadata.Country,
            City = metadata.City,
            Category = metadata.Category,
            TrustedStores = metadata.TrustedStores,
            SecondHandSites = metadata.SecondHandSites,
            ReportEmail = metadata.ReportEmail
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        LoadSelectLists();

        if (!ModelState.IsValid) return Page();

        ValidateProductInputs();
        if (!ModelState.IsValid) return Page();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        var wasEnabled = alert.IsEnabled;
        var sourceUrl = NormalizeSource(Input.Symbol);
        var timeframe = NormalizeTimeframe(Input.Timeframe);
        var channel = NormalizeChannel(Input.Channel);
        var metadata = BuildMetadata().Serialize();

        var duplicateExists = await _db.Alerts.AnyAsync(a =>
            a.Id != alert.Id &&
            a.UserId == userId &&
            a.Symbol == sourceUrl &&
            a.MarketType == Input.MarketType &&
            a.RuleType == Input.RuleType &&
            a.Threshold == Input.Threshold &&
            a.Timeframe == timeframe &&
            a.ZonePercent == Input.ZonePercent &&
            a.Channel == channel &&
            a.AudienceList == metadata);

        if (duplicateExists)
        {
            ModelState.AddModelError(string.Empty, "An identical alert already exists for this product.");
            return Page();
        }

        if (!wasEnabled && Input.IsEnabled)
        {
            var limits = await _accounts.GetLimitsAsync(userId);
            if (!limits.IsUnlimited && limits.RemainingSlots <= 0)
            {
                ModelState.AddModelError(string.Empty, "No active credits available. Pause another product or upgrade the plan.");
                return Page();
            }
        }

        alert.Symbol = sourceUrl;
        alert.MarketType = Input.MarketType;
        alert.RuleType = Input.RuleType;
        alert.Threshold = Input.Threshold;
        alert.Timeframe = timeframe;
        alert.ZonePercent = Input.ZonePercent;
        alert.RsiPeriod = 14;
        alert.FastEmaPeriod = 3;
        alert.SlowEmaPeriod = 5;
        alert.CooldownMinutes = Input.RuleType == AlertRuleType.DailyOpportunityReport ? 1440 : Input.CooldownMinutes;
        alert.IsEnabled = Input.IsEnabled;
        alert.Channel = channel;
        alert.AudienceList = metadata;
        alert.IndicatorArmed = false;
        alert.LastIndicatorValue = null;
        alert.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/App/Alerts/Index");
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
            ModelState.AddModelError("Input.Symbol", "Enter a valid product URL.");
        }

        if (!SupportedChannels.Contains(Input.Channel ?? string.Empty))
            ModelState.AddModelError("Input.Channel", "Choose Email, Webhook, Slack, Discord, Teams, or Telegram.");

        if (!string.IsNullOrWhiteSpace(Input.ReportEmail) && !Input.ReportEmail.Contains('@', StringComparison.Ordinal))
            ModelState.AddModelError("Input.ReportEmail", "Enter a valid email address for reports.");
    }

    private void NormalizeRuleInputs()
    {
        Input.Timeframe = NormalizeTimeframe(Input.Timeframe);
        Input.Symbol = NormalizeSource(Input.Symbol);
        Input.Country = NormalizeText(Input.Country, ProductWatchMetadata.Default.Country);
        Input.City = NormalizeText(Input.City, ProductWatchMetadata.Default.City);
        Input.Category = NormalizeText(Input.Category, ProductWatchMetadata.Default.Category);
        Input.TrustedStores = NormalizeText(Input.TrustedStores, ProductWatchMetadata.Default.TrustedStores);
        Input.SecondHandSites = NormalizeText(Input.SecondHandSites, ProductWatchMetadata.Default.SecondHandSites);

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
        return new ProductWatchMetadata(
            NormalizeText(Input.Country, ProductWatchMetadata.Default.Country),
            NormalizeText(Input.City, string.Empty),
            NormalizeText(Input.Category, ProductWatchMetadata.Default.Category),
            NormalizeText(Input.TrustedStores, ProductWatchMetadata.Default.TrustedStores),
            NormalizeText(Input.SecondHandSites, ProductWatchMetadata.Default.SecondHandSites),
            NormalizeText(Input.ReportEmail, string.Empty));
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

    private static string NormalizeSource(string? source) => (source ?? string.Empty).Trim();

    private static string NormalizeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
