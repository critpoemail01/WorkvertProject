using System.ComponentModel.DataAnnotations;
using Alivert.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Alivert.Controllers;

[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accountService;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IUserAccountService accountService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountService = accountService;
    }

    public sealed class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public sealed class RegisterRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return BadRequest(new { message = "Invalid credentials." });

        var result = await _signInManager.PasswordSignInAsync(user, req.Password, req.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return StatusCode(429, new { message = "Too many failed attempts. Account temporarily locked." });

        if (!result.Succeeded)
            return BadRequest(new { message = "Invalid credentials." });

        await _accountService.EnsureAsync(user.Id);

        var redirect = SafeReturnUrl(req.ReturnUrl) ?? Url.Page("/App/Dashboard")!;
        return Ok(new { redirect });
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterRequest req)
    {
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing is not null)
            return BadRequest(new { message = "This email is already registered." });

        var user = new IdentityUser { UserName = req.Email, Email = req.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var msg = result.Errors.FirstOrDefault()?.Description ?? "Could not create the account.";
            return BadRequest(new { message = msg });
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        await _accountService.EnsureAsync(user.Id);

        var redirect = SafeReturnUrl(req.ReturnUrl) ?? Url.Page("/App/Dashboard")!;
        return Ok(new { redirect });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { redirect = Url.Page("/Index")! });
    }

    private static string? SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        // Only allow local URLs (avoid open redirects)
        return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") ? returnUrl : null;
    }
}
