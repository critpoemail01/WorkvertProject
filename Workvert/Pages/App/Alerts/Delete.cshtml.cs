using Workvert.Data;
using Workvert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Workvert.Pages.App.Alerts;

[Authorize]
public class DeleteAlertModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public DeleteAlertModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public string Symbol { get; private set; } = string.Empty;
    public string RuleType { get; private set; } = string.Empty;
    public decimal Threshold { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        Symbol = alert.Symbol;
        RuleType = alert.RuleType.DisplayName();
        Threshold = alert.Threshold;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        // Optional: delete activity history too.
        var triggers = await _db.AlertTriggers.Where(t => t.AlertId == id).ToListAsync();
        _db.AlertTriggers.RemoveRange(triggers);

        _db.Alerts.Remove(alert);
        await _db.SaveChangesAsync();

        return RedirectToPage("/App/Alerts/Index");
    }
}
