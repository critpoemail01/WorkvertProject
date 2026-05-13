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
public class EditAlertModel : PageModel
{

    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email",
        "Telegram",
        "Discord",
        "Webhook"
    };


    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;
    private readonly ISymbolCatalogService _symbols;

    public EditAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts, ISymbolCatalogService symbols)
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

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "Symbol")]
        [Required, StringLength(32), RegularExpression(@"^[A-Za-z0-9.^=_-]{1,32}$", ErrorMessage = "Symbol may contain letters, digits, dot, caret, equals, underscore, dash only.")]
        public string Symbol { get; set; } = "BTCUSDT";

        [Display(Name = "Market")]
        [Required]
        public MarketType MarketType { get; set; } = MarketType.Crypto;

        [Display(Name = "Rule")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceAbove;

        [Display(Name = "Threshold")]
        [Required]
        public decimal Threshold { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        LoadSelectLists();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        Input = new InputModel
        {
            Id = alert.Id,
            Symbol = alert.Symbol,
            MarketType = alert.MarketType,
            RuleType = alert.RuleType,
            Threshold = alert.Threshold,
            Timeframe = alert.Timeframe,
            ZonePercent = alert.ZonePercent,
            RsiPeriod = alert.RsiPeriod,
            FastEmaPeriod = alert.FastEmaPeriod,
            SlowEmaPeriod = alert.SlowEmaPeriod,
            CooldownMinutes = alert.CooldownMinutes,
            IsEnabled = alert.IsEnabled,
            Channel = alert.Channel
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        LoadSelectLists();

        if (!ModelState.IsValid) return Page();

        ValidateRuleSpecificInputs();
        if (!ModelState.IsValid) return Page();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        var wasEnabled = alert.IsEnabled;
        var symbol = (Input.Symbol ?? "").Trim().ToUpperInvariant();
        var timeframe = NormalizeTimeframe(Input.Timeframe);

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
            ModelState.AddModelError("Input.Channel", "Unsupported channel. Allowed: Email, Telegram, Discord, Webhook.");
            return Page();
        }

        var duplicateExists = await _db.Alerts.AnyAsync(a =>
            a.Id != alert.Id &&
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
        if (duplicateExists)
        {
            ModelState.AddModelError(string.Empty, "An identical alert already exists.");
            return Page();
        }

        // Enabling an alert consumes a slot on the Free plan.
        if (!wasEnabled && Input.IsEnabled)
        {
            var limits = await _accounts.GetLimitsAsync(userId);
            if (!limits.IsUnlimited && limits.RemainingSlots <= 0)
            {
                ModelState.AddModelError(string.Empty, "No credits available. Disable an existing alert or upgrade your plan.");
                return Page();
            }
        }

        alert.Symbol = symbol;
        alert.MarketType = Input.MarketType;
        alert.RuleType = Input.RuleType;
        alert.Threshold = Input.Threshold;
        alert.Timeframe = timeframe;
        alert.ZonePercent = Input.ZonePercent;
        alert.RsiPeriod = Input.RsiPeriod;
        alert.FastEmaPeriod = Input.FastEmaPeriod;
        alert.SlowEmaPeriod = Input.SlowEmaPeriod;
        alert.CooldownMinutes = Input.CooldownMinutes;
        alert.IsEnabled = Input.IsEnabled;
        alert.Channel = channel;
        alert.IndicatorArmed = false;
        alert.LastIndicatorValue = null;

        alert.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/App/Alerts/Index");
    }

    private void LoadSelectLists()
    {
        RuleTypeOptions = new SelectList(Enum.GetValues<AlertRuleType>());
        TimeframeOptions = new SelectList(new[] { "5m", "15m", "1h", "4h", "1d", "1wk", "1mo" });
        MarketTypeOptions = new SelectList(Enum.GetValues<MarketType>());
    }

    private void ValidateRuleSpecificInputs()
    {
        Input.Timeframe = NormalizeTimeframe(Input.Timeframe);

        if (Input.RuleType.RequiresTechnicalIndicators())
        {
            if (Input.Threshold < 0 || Input.Threshold > 100)
                ModelState.AddModelError("Input.Threshold", "RSI threshold must be between 0 and 100.");

            if (Input.FastEmaPeriod >= Input.SlowEmaPeriod)
                ModelState.AddModelError("Input.SlowEmaPeriod", "Slow EMA must be greater than fast EMA.");
        }

        if (Input.RuleType.UsesPriceZone() && Input.Threshold <= 0)
            ModelState.AddModelError("Input.Threshold", "Price zone center must be greater than zero.");
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
