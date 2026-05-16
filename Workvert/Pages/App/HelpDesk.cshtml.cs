using System.ComponentModel.DataAnnotations;
using Workvert.Data;
using Workvert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Workvert.Pages.App;

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
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(120, ErrorMessage = "Name can be up to 120 characters.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the email address where support should reply.")]
        [EmailAddress(ErrorMessage = "Use a valid email address.")]
        [StringLength(256, ErrorMessage = "Email can be up to 256 characters.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Add a short subject.")]
        [StringLength(160, ErrorMessage = "Subject can be up to 160 characters.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describe the issue so support can help you.")]
        [StringLength(4000, ErrorMessage = "Message can be up to 4000 characters.")]
        [Display(Name = "Message")]
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
        {
            ModelState.AddModelError(string.Empty, "Complete the required ticket details before submitting.");
            return Page();
        }

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
