using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.Users;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class AccountController : Controller
{
    private readonly UserManager<IUser> _userManager;
    private readonly SignInManager<IUser> _userSignInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<IUser> userManager, SignInManager<IUser> userSignInManager, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _userSignInManager = userSignInManager;
        _logger = logger;
    }

    [AllowAnonymous]
    public async Task<IActionResult> SignInDirectly(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);

        _logger.LogWarning("UserName to be found: {UserName} User found: {User}", userName, user);

        if (user == null) return NotFound();

        await _userSignInManager.SignInAsync(user, isPersistent: false);

        return Ok();
    }

    public async Task<IActionResult> SignOutDirectly()
    {
        await _userSignInManager.SignOutAsync();

        return Ok();
    }
}
