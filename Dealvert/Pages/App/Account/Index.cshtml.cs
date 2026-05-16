using System.ComponentModel.DataAnnotations;
using Dealvert.Data;
using Dealvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Dealvert.Pages.App.Account;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IUserAccountService _accounts;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _accounts = accounts;
    }

    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime RegisteredSince { get; private set; }
    public bool HasPassword { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostContactAsync(string email, string? phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        email = email.Trim();
        phoneNumber = Clean(phoneNumber);

        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            ModelState.AddModelError(string.Empty, "Use a valid email address.");

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null && existing.Id != user.Id)
            ModelState.AddModelError(string.Empty, "This email is already registered.");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            Email = email;
            PhoneNumber = phoneNumber;
            return Page();
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, email);
            if (!setEmail.Succeeded)
                return await ErrorPageAsync(setEmail);

            var setUserName = await _userManager.SetUserNameAsync(user, email);
            if (!setUserName.Succeeded)
                return await ErrorPageAsync(setUserName);

            user.EmailConfirmed = true;
        }

        var setPhone = await _userManager.SetPhoneNumberAsync(user, phoneNumber);
        if (!setPhone.Succeeded)
            return await ErrorPageAsync(setPhone);

        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        StatusMessage = "Account details updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPasswordAsync(string? currentPassword, string? newPassword, string? confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        newPassword = newPassword?.Trim();
        confirmPassword = confirmPassword?.Trim();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            ModelState.AddModelError(string.Empty, "New password must be at least 6 characters.");

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            ModelState.AddModelError(string.Empty, "Password confirmation does not match.");

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (hasPassword && string.IsNullOrWhiteSpace(currentPassword))
            ModelState.AddModelError(string.Empty, "Current password is required.");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var result = hasPassword
            ? await _userManager.ChangePasswordAsync(user, currentPassword!, newPassword!)
            : await _userManager.AddPasswordAsync(user, newPassword!);

        if (!result.Succeeded)
            return await ErrorPageAsync(result);

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = hasPassword ? "Password updated." : "Password added to your account.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return;

        await _accounts.EnsureAsync(user.Id);

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        Email = user.Email ?? user.UserName ?? string.Empty;
        PhoneNumber = user.PhoneNumber;
        RegisteredSince = account?.CreatedAtUtc ?? DateTime.UtcNow;
        HasPassword = await _userManager.HasPasswordAsync(user);
    }

    private async Task<IActionResult> ErrorPageAsync(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        await LoadAsync();
        return Page();
    }

    private static string? Clean(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
