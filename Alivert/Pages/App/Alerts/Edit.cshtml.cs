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

    public EditAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList RuleTypeOptions { get; private set; } = default!;

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "Symbol")]
        [Required, StringLength(24), RegularExpression(@"^[A-Z0-9._-]{1,24}$", ErrorMessage = "Symbol may contain A-Z, 0-9, dot, underscore, dash only.")]
        public string Symbol { get; set; } = "AAPL";

        [Display(Name = "Rule")]
        [Required]
        public AlertRuleType RuleType { get; set; } = AlertRuleType.PriceAbove;

        [Display(Name = "Threshold")]
        [Required]
        public decimal Threshold { get; set; }

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
        RuleTypeOptions = new SelectList(Enum.GetValues<AlertRuleType>());

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        var wasEnabled = alert.IsEnabled;
        Input = new InputModel
        {
            Id = alert.Id,
            Symbol = alert.Symbol,
            RuleType = alert.RuleType,
            Threshold = alert.Threshold,
            CooldownMinutes = alert.CooldownMinutes,
            IsEnabled = alert.IsEnabled,
            Channel = alert.Channel
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        RuleTypeOptions = new SelectList(Enum.GetValues<AlertRuleType>());

        if (!ModelState.IsValid) return Page();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        var wasEnabled = alert.IsEnabled;
        alert.Symbol = (Input.Symbol ?? "").Trim().ToUpperInvariant();
        alert.RuleType = Input.RuleType;
        alert.Threshold = Input.Threshold;
        alert.CooldownMinutes = Input.CooldownMinutes;
        var channel = (Input.Channel ?? "Email").Trim();
        if (!SupportedChannels.Contains(channel))
        {
            ModelState.AddModelError(nameof(Input.Channel), "Unsupported channel. Allowed: Email, Telegram, Discord, Webhook.");
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

        alert.IsEnabled = Input.IsEnabled;
        alert.Channel = channel;

        alert.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/App/Alerts/Index");
    }
}
