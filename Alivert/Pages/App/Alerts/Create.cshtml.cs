using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Alivert.Pages.App.Alerts;

[Authorize]
public class CreateAlertModel : PageModel
{

    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email",
        "TikTok",
        "Instagram",
        "Facebook",
        "LinkedIn",
        "SMS",
        "Webhook"
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
        [Display(Name = "URL, company or business idea")]
        [Required, StringLength(180, ErrorMessage = "Use 180 characters or fewer.")]
        public string Symbol { get; set; } = "https://example.com";

        [Display(Name = "Source type")]
        [Required]
        public MarketType MarketType { get; set; } = MarketType.Crypto;

        [Display(Name = "Campaign asset")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceAbove;

        [Display(Name = "Goal")]
        [Required]
        public decimal Threshold { get; set; } = 1000m;

        [Display(Name = "Cadence")]
        [Required, StringLength(8)]
        public string Timeframe { get; set; } = "1wk";

        [Display(Name = "Offer strength")]
        [Range(0.01, 100)]
        public decimal ZonePercent { get; set; } = 10.0m;

        [Display(Name = "Audience segment")]
        [Range(2, 100)]
        public int RsiPeriod { get; set; } = 14;

        [Display(Name = "Creative variants")]
        [Range(1, 200)]
        public int FastEmaPeriod { get; set; } = 3;

        [Display(Name = "Follow-up steps")]
        [Range(2, 250)]
        public int SlowEmaPeriod { get; set; } = 5;

        [Display(Name = "Follow-up delay (min)")]
        [Range(1, 1440)]
        public int CooldownMinutes { get; set; } = 240;

        [Display(Name = "Active")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Primary channel")]
        [StringLength(32)]
        public string Channel { get; set; } = "Email";

        [Display(Name = "Potential client list")]
        [StringLength(4000, ErrorMessage = "Use 4000 characters or fewer.")]
        public string? AudienceList { get; set; }
    }

    public async Task OnGetAsync(MarketType? marketType = null, string? symbol = null, AlertRuleType? ruleType = null, string? timeframe = null, decimal? threshold = null)
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

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimited = limits.IsUnlimited;
        RemainingSlots = limits.RemainingSlots;
        ActiveAlerts = limits.ActiveAlerts;
        Capacity = limits.Capacity;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadSelectLists();

        var userId = _userManager.GetUserId(User) ?? string.Empty;

        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimited = limits.IsUnlimited;
        RemainingSlots = limits.RemainingSlots;
        ActiveAlerts = limits.ActiveAlerts;
        Capacity = limits.Capacity;

        if (!ModelState.IsValid)
            return Page();

        ValidateRuleSpecificInputs();
        if (!ModelState.IsValid)
            return Page();

        if (Input.IsEnabled && !IsUnlimited && RemainingSlots <= 0)
        {
            ModelState.AddModelError(string.Empty, "No campaign credits available. Buy a credit pack or upgrade before activating more campaigns.");
            return Page();
        }
        var symbol = NormalizeSource(Input.Symbol);

        
        var channel = (Input.Channel ?? "Email").Trim();
        if (!SupportedChannels.Contains(channel))
        {
            ModelState.AddModelError("Input.Channel", "Unsupported channel. Allowed: Email, TikTok, Instagram, Facebook, LinkedIn, SMS, Webhook.");
            return Page();
        }

        // Optional: prevent duplicate campaign briefs for the same source and asset.
        var timeframe = NormalizeTimeframe(Input.Timeframe);
        var audienceList = CleanAudienceList(Input.AudienceList);
        var exists = await _db.Alerts.AnyAsync(a =>
            a.UserId == userId &&
            a.Symbol == symbol &&
            a.MarketType == Input.MarketType &&
            a.RuleType == Input.RuleType &&
            a.Threshold == Input.Threshold &&
            a.Timeframe == timeframe &&
            a.ZonePercent == Input.ZonePercent &&
            a.RsiPeriod == Input.RsiPeriod &&
            a.FastEmaPeriod == Input.FastEmaPeriod &&
            a.SlowEmaPeriod == Input.SlowEmaPeriod &&
            a.AudienceList == audienceList);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "An identical campaign already exists.");
            return Page();
        }

        _db.Alerts.Add(new Alert
        {
            UserId = userId,
            Symbol = symbol,
            MarketType = Input.MarketType,
            RuleType = Input.RuleType,
            Threshold = Input.Threshold,
            Timeframe = timeframe,
            ZonePercent = Input.ZonePercent,
            RsiPeriod = Input.RsiPeriod,
            FastEmaPeriod = Input.FastEmaPeriod,
            SlowEmaPeriod = Input.SlowEmaPeriod,
            CooldownMinutes = Input.CooldownMinutes,
            IsEnabled = Input.IsEnabled,
            Channel = channel,
            AudienceList = audienceList
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/App/Alerts/Index");
    }

    private void LoadSelectLists()
    {
        RuleTypeOptions = new SelectList(
            Enum.GetValues<AlertRuleType>().Select(rule => new { Value = rule.ToString(), Text = rule.DisplayName() }),
            "Value",
            "Text");
        TimeframeOptions = new SelectList(new[]
        {
            new { Value = "1d", Text = "Daily" },
            new { Value = "3d", Text = "Every 3 days" },
            new { Value = "1wk", Text = "Weekly" },
            new { Value = "2wk", Text = "Biweekly" },
            new { Value = "1mo", Text = "Monthly" }
        }, "Value", "Text");
        MarketTypeOptions = new SelectList(new[]
        {
            new { Value = MarketType.Crypto.ToString(), Text = "Application URL" },
            new { Value = MarketType.Traditional.ToString(), Text = "Company or idea" }
        }, "Value", "Text");
    }

    private void ValidateRuleSpecificInputs()
    {
        NormalizeRuleInputs();

        if (string.IsNullOrWhiteSpace(Input.Symbol))
            ModelState.AddModelError("Input.Symbol", "Add the application URL, company name or business idea.");

        if (Input.RuleType.UsesEma() && Input.FastEmaPeriod >= Input.SlowEmaPeriod)
            ModelState.AddModelError("Input.SlowEmaPeriod", "Follow-up steps must be greater than creative variants.");

        if (Input.Threshold < 0)
            ModelState.AddModelError("Input.Threshold", "Campaign goal must be zero or greater.");
    }

    private void NormalizeRuleInputs()
    {
        Input.Timeframe = NormalizeTimeframe(Input.Timeframe);

        if (!Input.RuleType.UsesThreshold())
            Input.Threshold = 0;

        if (!Input.RuleType.UsesPriceZone())
            Input.ZonePercent = 1.0m;

        if (!Input.RuleType.UsesRsi())
            Input.RsiPeriod = 14;

        if (!Input.RuleType.UsesEma())
        {
            Input.FastEmaPeriod = 3;
            Input.SlowEmaPeriod = 5;
        }
    }

    private static string NormalizeTimeframe(string? timeframe)
    {
        return timeframe?.Trim().ToLowerInvariant() switch
        {
            "1d" => "1d",
            "3d" => "3d",
            "1wk" or "1w" => "1wk",
            "2wk" or "2w" => "2wk",
            "1mo" => "1mo",
            _ => "1wk"
        };
    }

    private static string NormalizeSource(string? source)
    {
        return (source ?? string.Empty).Trim();
    }

    private static string? CleanAudienceList(string? audienceList)
    {
        return string.IsNullOrWhiteSpace(audienceList) ? null : audienceList.Trim();
    }
}
