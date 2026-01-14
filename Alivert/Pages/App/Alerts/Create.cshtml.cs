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

    public bool IsUnlimited { get; private set; }
    public int RemainingSlots { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int Capacity { get; private set; }

    public class InputModel
    {
        [Display(Name = "Symbol")]
        [Required, StringLength(24), RegularExpression(@"^[A-Z0-9._-]{1,24}$", ErrorMessage = "Symbol may contain A-Z, 0-9, dot, underscore, dash only.")]
        public string Symbol { get; set; } = "AAPL";

        [Display(Name = "Rule")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceAbove;

        [Display(Name = "Threshold")]
        [Required]
        public decimal Threshold { get; set; } = 100m;

        [Display(Name = "Cooldown (min)")]
        [Range(1, 1440)]
        public int CooldownMinutes { get; set; } = 240;

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Channel")]
        [StringLength(32)]
        public string Channel { get; set; } = "Email";
    }

    public async Task OnGetAsync()
    {
        RuleTypeOptions = new SelectList(Enum.GetValues<AlertRuleType>());

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimited = limits.IsUnlimited;
        RemainingSlots = limits.RemainingSlots;
        ActiveAlerts = limits.ActiveAlerts;
        Capacity = limits.Capacity;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        RuleTypeOptions = new SelectList(Enum.GetValues<AlertRuleType>());

        if (!ModelState.IsValid)
            return Page();

        var userId = _userManager.GetUserId(User) ?? string.Empty;

        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimited = limits.IsUnlimited;
        RemainingSlots = limits.RemainingSlots;
        ActiveAlerts = limits.ActiveAlerts;
        Capacity = limits.Capacity;

        if (Input.IsEnabled && !IsUnlimited && RemainingSlots <= 0)
        {
            ModelState.AddModelError(string.Empty, "No credits available to activate more alerts. Buy a credit pack or upgrade to the Unlimited plan.");
            return Page();
        }
        var symbol = (Input.Symbol ?? "").Trim().ToUpperInvariant();

        
        var channel = (Input.Channel ?? "Email").Trim();
        if (!SupportedChannels.Contains(channel))
        {
            ModelState.AddModelError(nameof(Input.Channel), "Unsupported channel. Allowed: Email, Telegram, Discord, Webhook.");
            return Page();
        }

// Optional: prevent duplicates by symbol+ruletype+threshold
        var exists = await _db.Alerts.AnyAsync(a => a.UserId == userId && a.Symbol == symbol && a.RuleType == Input.RuleType && a.Threshold == Input.Threshold);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "An identical alert already exists.");
            return Page();
        }

        _db.Alerts.Add(new Alert
        {
            UserId = userId,
            Symbol = symbol,
            RuleType = Input.RuleType,
            Threshold = Input.Threshold,
            CooldownMinutes = Input.CooldownMinutes,
            IsEnabled = Input.IsEnabled,
            Channel = channel
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/App/Alerts/Index");
    }
}
