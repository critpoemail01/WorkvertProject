using Workvert.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Workvert.ViewComponents;

public sealed class AppPlanCtaViewComponent : ViewComponent
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public AppPlanCtaViewComponent(UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _userManager = userManager;
        _accounts = accounts;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(ViewContext.HttpContext.User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Content(string.Empty);
        }

        var limits = await _accounts.GetLimitsAsync(userId, ViewContext.HttpContext.RequestAborted);
        return View(new AppPlanCtaModel(limits.IsUnlimited));
    }

    public sealed record AppPlanCtaModel(bool IsUnlimited);
}
