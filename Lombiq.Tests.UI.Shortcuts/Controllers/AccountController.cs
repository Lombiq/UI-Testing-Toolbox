using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Users;
using OrchardCore.Users.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class AccountController : Controller
{
    private readonly UserManager<IUser> _userManager;
    private readonly SignInManager<IUser> _userSignInManager;
    private readonly IUserService _userService;

    public AccountController(UserManager<IUser> userManager, SignInManager<IUser> userSignInManager, IUserService userService)
    {
        _userManager = userManager;
        _userSignInManager = userSignInManager;
        _userService = userService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> SignInDirectly(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);

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
