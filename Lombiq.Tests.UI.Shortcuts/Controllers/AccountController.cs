using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Users;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class AccountController(UserManager<IUser> userManager, SignInManager<IUser> userSignInManager) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> SignInDirectly(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) userName = "admin";
        if (await userManager.FindByNameAsync(userName) is not { } user) return NotFound();

        await userSignInManager.SignInAsync(user, isPersistent: false);

        return Ok();
    }

    public async Task<IActionResult> SignOutDirectly()
    {
        await userSignInManager.SignOutAsync();

        return Ok();
    }
}
