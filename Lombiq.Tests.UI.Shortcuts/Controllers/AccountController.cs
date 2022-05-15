using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Entities;
using OrchardCore.Settings;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class AccountController : Controller
{
    private readonly UserManager<IUser> _userManager;
    private readonly SignInManager<IUser> _userSignInManager;
    private readonly ISiteService _siteService;

    public AccountController(
        UserManager<IUser> userManager,
        SignInManager<IUser> userSignInManager,
        ISiteService siteService)
    {
        _userManager = userManager;
        _userSignInManager = userSignInManager;
        _siteService = siteService;
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
        var registrationSettings = settings.As<RegistrationSettings>();

        registrationSettings.UsersCanRegister = type;

        settings.Put(registrationSettings);

        await _siteService.UpdateSiteSettingsAsync(settings);

        return Ok();
    }
}
