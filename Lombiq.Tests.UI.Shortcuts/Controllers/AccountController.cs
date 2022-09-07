using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Entities;
using OrchardCore.Settings;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class AccountController : Controller
{
    private readonly UserManager<IUser> _userManager;
    private readonly SignInManager<IUser> _userSignInManager;
    private readonly ISiteService _siteService;
    private readonly IUserService _userService;

    public AccountController(
        UserManager<IUser> userManager,
        SignInManager<IUser> userSignInManager,
        ISiteService siteService,
        IUserService userService)
    {
        _userManager = userManager;
        _userSignInManager = userSignInManager;
        _siteService = siteService;
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

    [AllowAnonymous]
    public async Task<ActionResult> SetUserRegistrationType(UserRegistrationType type)
    {
        var settings = await _siteService.LoadSiteSettingsAsync();

        settings.Alter<RegistrationSettings>(
            nameof(RegistrationSettings),
            registrationSettings => registrationSettings.UsersCanRegister = type);

        await _siteService.UpdateSiteSettingsAsync(settings);

        return Ok();
    }

    [AllowAnonymous]
    public async Task<ActionResult> CreateUser([FromJsonQueryString] CreateUserRequest userData)
    {
        var user = await _userService.CreateUserAsync(
            new User
            {
                UserName = userData.UserName,
                Email = userData.Email,
                EmailConfirmed = true,
                IsEnabled = true,
            },
            userData.Password,
            (key, error) => ModelState.AddModelError(key, error));

        if (user == null)
        {
            return BadRequest(ModelState);
        }

        return Ok("Success");
    }
}
