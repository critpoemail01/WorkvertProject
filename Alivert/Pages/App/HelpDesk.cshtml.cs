using System.ComponentModel.DataAnnotations;
using Alivert.Data;
using Alivert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App;

[Authorize]
public class HelpDeskModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public HelpDeskModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SupportTicket> Tickets { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string Subject { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Message { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadTicketsAsync();

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var ticket = new SupportTicket
        {
            UserId = user.Id,
            Name = Clean(Input.Name) ?? string.Empty,
            Email = Clean(Input.Email) ?? string.Empty,
            Subject = Clean(Input.Subject) ?? string.Empty,
            Message = Clean(Input.Message) ?? string.Empty
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync();

        StatusMessage = $"Support ticket #{ticket.Id} opened. We will reply to {ticket.Email}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return;

        Input = new InputModel
        {
            Name = user.UserName ?? user.Email ?? string.Empty,
            Email = user.Email ?? string.Empty
        };

        await LoadTicketsAsync(user.Id);
    }

    private async Task LoadTicketsAsync(string? userId = null)
    {
        userId ??= _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        Tickets = await _db.SupportTickets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .ToListAsync();
    }

    private static string? Clean(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
