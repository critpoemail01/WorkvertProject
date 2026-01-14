using Alivert.Data;
using Alivert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App.Alerts;

[Authorize]
public class AlertsIndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public AlertsIndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Alert> Alerts { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        Alerts = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsEnabled)
            .ThenBy(a => a.Symbol)
            .ToListAsync();
    }
}
