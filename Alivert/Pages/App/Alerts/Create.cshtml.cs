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
        "Telegram",
        "Discord",
        "Slack",
        "Teams",
        "Webhook"
    };


    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;
    private readonly ISymbolCatalogService _symbols;

    public CreateAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts, ISymbolCatalogService symbols)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
        _symbols = symbols;
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
        [Display(Name = "Symbol")]
        [Required, StringLength(32), RegularExpression(@"^[A-Za-z0-9.^=_-]{1,32}$", ErrorMessage = "Symbol may contain letters, digits, dot, caret, equals, underscore, dash only.")]
        public string Symbol { get; set; } = "BTCUSDT";

        [Display(Name = "Market")]
        [Required]
        public MarketType MarketType { get; set; } = MarketType.Crypto;

        [Display(Name = "Alert type")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceAbove;

        [Display(Name = "Threshold")]
        [Required]
        public decimal Threshold { get; set; } = 100m;

        [Display(Name = "Timeframe")]
        [Required, StringLength(8)]
        public string Timeframe { get; set; } = "4h";

        [Display(Name = "Zone percent")]
        [Range(0.01, 100)]
        public decimal ZonePercent { get; set; } = 1.0m;

        [Display(Name = "RSI period")]
        [Range(2, 100)]
        public int RsiPeriod { get; set; } = 14;

        [Display(Name = "Fast EMA")]
        [Range(1, 200)]
        public int FastEmaPeriod { get; set; } = 3;

        [Display(Name = "Slow EMA")]
        [Range(2, 250)]
        public int SlowEmaPeriod { get; set; } = 5;

        [Display(Name = "Cooldown (min)")]
        [Range(1, 1440)]
        public int CooldownMinutes { get; set; } = 240;

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Channel")]
        [StringLength(32)]
        public string Channel { get; set; } = "Email";
    }

    public async Task OnGetAsync(MarketType? marketType = null, string? symbol = null, AlertRuleType? ruleType = null, string? timeframe = null, decimal? threshold = null)
    {
        LoadSelectLists();

        if (marketType is not null)
            Input.MarketType = marketType.Value;

        if (!string.IsNullOrWhiteSpace(symbol))
            Input.Symbol = symbol.Trim().ToUpperInvariant();

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
            ModelState.AddModelError(string.Empty, "No credits available to activate more alerts. Buy a credit pack or upgrade to the Unlimited plan.");
            return Page();
        }
        var symbol = (Input.Symbol ?? "").Trim().ToUpperInvariant();

        if (!await _symbols.IsValidSymbolAsync(Input.MarketType, symbol, HttpContext.RequestAborted))
        {
            ModelState.AddModelError("Input.Symbol", Input.MarketType == MarketType.Crypto
                ? "Choose a valid Binance spot symbol."
                : "Choose a symbol available on Yahoo Finance.");
            return Page();
        }

        
        var channel = (Input.Channel ?? "Email").Trim();
        if (!SupportedChannels.Contains(channel))
        {
            ModelState.AddModelError("Input.Channel", "Unsupported channel. Allowed: Email, Telegram, Discord, Slack, Teams, Webhook.");
            return Page();
        }

        // Optional: prevent duplicates by symbol+rule type+threshold
        var timeframe = NormalizeTimeframe(Input.Timeframe);
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
            a.SlowEmaPeriod == Input.SlowEmaPeriod);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "An identical alert already exists.");
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
            Channel = channel
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
        TimeframeOptions = new SelectList(new[] { "5m", "15m", "1h", "4h", "1d", "1wk", "1mo" });
        MarketTypeOptions = new SelectList(Enum.GetValues<MarketType>());
    }

    private void ValidateRuleSpecificInputs()
    {
        NormalizeRuleInputs();

        if (Input.RuleType.UsesRsi() && (Input.Threshold < 0 || Input.Threshold > 100))
            ModelState.AddModelError("Input.Threshold", "RSI threshold must be between 0 and 100.");

        if (Input.RuleType.UsesEma() && Input.FastEmaPeriod >= Input.SlowEmaPeriod)
            ModelState.AddModelError("Input.SlowEmaPeriod", "Slow EMA must be greater than fast EMA.");

        if (Input.RuleType is AlertRuleType.PriceAbove or AlertRuleType.PriceBelow && Input.Threshold <= 0)
            ModelState.AddModelError("Input.Threshold", "Price level must be greater than zero.");

        if (Input.RuleType.UsesPriceZone() && Input.Threshold <= 0)
            ModelState.AddModelError("Input.Threshold", "Price zone center must be greater than zero.");

        if (Input.RuleType == AlertRuleType.VolumeAbove24h && Input.Threshold <= 0)
            ModelState.AddModelError("Input.Threshold", "Volume threshold must be greater than zero.");

        if (Input.RuleType == AlertRuleType.PercentDrop24h && Input.Threshold >= 0)
            ModelState.AddModelError("Input.Threshold", "Use a negative value for a 24h percent drop, for example -3.");

        if (Input.RuleType == AlertRuleType.PercentRise24h && Input.Threshold <= 0)
            ModelState.AddModelError("Input.Threshold", "Use a positive value for a 24h percent rise, for example 3.");
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
            "5m" => "5m",
            "15m" => "15m",
            "1h" => "1h",
            "4h" => "4h",
            "1d" => "1d",
            "1wk" or "1w" => "1wk",
            "1mo" => "1mo",
            _ => "4h"
        };
    }
}
