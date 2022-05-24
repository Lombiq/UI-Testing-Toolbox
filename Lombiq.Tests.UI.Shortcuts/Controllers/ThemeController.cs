using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules.Manifest;
using OrchardCore.Themes.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class ThemeController : Controller
{
    private readonly IShellFeaturesManager _shellFeaturesManager;
    private readonly ISiteThemeService _siteThemeService;
    private readonly IAdminThemeService _adminThemeService;

    public ThemeController(
        IShellFeaturesManager shellFeaturesManager,
        ISiteThemeService siteThemeService,
        IAdminThemeService adminThemeService)
    {
        _shellFeaturesManager = shellFeaturesManager;
        _siteThemeService = siteThemeService;
        _adminThemeService = adminThemeService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> SelectTheme(string id)
    {
        var themeFeature = (await _shellFeaturesManager.GetAvailableFeaturesAsync())
            .FirstOrDefault(feature => feature.IsTheme() && feature.Id == id);

        if (themeFeature == null)
        {
            return NotFound();
        }

        if (IsAdminTheme(themeFeature.Extension.Manifest))
        {
            await _adminThemeService.SetAdminThemeAsync(id);
        }
        else
        {
            await _siteThemeService.SetSiteThemeAsync(id);
        }

        var enabledFeatures = await _shellFeaturesManager.GetEnabledFeaturesAsync();
        var isEnabled = enabledFeatures.Any(feature => feature.Extension.Id == themeFeature.Id);

        if (!isEnabled)
        {
            await _shellFeaturesManager.EnableFeaturesAsync(new[] { themeFeature }, force: true);
        }

        return Ok();
    }

    private static bool IsAdminTheme(IManifestInfo manifest) =>
        manifest.Tags.Any(tag => tag.EqualsOrdinalIgnoreCase(ManifestConstants.AdminTag));
}
