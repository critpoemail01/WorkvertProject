using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
    private readonly IConfiguration _configuration;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IUserAccountService accountService,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountService = accountService;
        _configuration = configuration;
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

        return Ok(new { redirect = DashboardUrl() });
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

        return Ok(new { redirect = DashboardUrl() });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { redirect = Url.Page("/Index")! });
    }

    [HttpGet("google")]
    public IActionResult Google([FromQuery] string? returnUrl = null)
    {
        if (!GoogleIsConfigured())
            return RedirectToPage("/Index", new { login = 1, externalError = "Google sign-in is not configured yet." });

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null, [FromQuery] string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
            return RedirectToPage("/Index", new { login = 1, externalError = "Google sign-in was cancelled or rejected." });

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return RedirectToPage("/Index", new { login = 1, externalError = "Could not read Google sign-in information." });

        var signIn = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signIn.Succeeded)
        {
            var existing = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existing is not null)
                await _accountService.EnsureAsync(existing.Id);

            return LocalRedirect(DashboardUrl());
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToPage("/Index", new { login = 1, externalError = "Google account did not provide an email address." });

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
                return RedirectToPage("/Index", new { login = 1, externalError = "Could not create the account from Google sign-in." });
        }

        var addLogin = await _userManager.AddLoginAsync(user, info);
        if (!addLogin.Succeeded && !addLogin.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
            return RedirectToPage("/Index", new { login = 1, externalError = "Could not attach Google sign-in to this account." });

        await _signInManager.SignInAsync(user, isPersistent: false);
        await _accountService.EnsureAsync(user.Id);
        return LocalRedirect(DashboardUrl());
    }

    private string DashboardUrl() => Url.Page("/App/Dashboard")!;

    private bool GoogleIsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);
    }
}
