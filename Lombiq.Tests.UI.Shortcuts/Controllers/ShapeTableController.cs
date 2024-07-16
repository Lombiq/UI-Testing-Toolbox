using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Theming;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

public class ShapeTableController : Controller
{
    /// <summary>
    /// Prepares the shape table for the current site and admin themes.
    /// </summary>
    public async Task<IActionResult> Prepare()
    {
        var provider = HttpContext.RequestServices;

        var shapeTableManager = provider.GetRequiredService<IShapeTableManager>();
        var siteTheme = await provider.GetRequiredService<IThemeManager>().GetThemeAsync();
        var adminTheme = await provider.GetRequiredService<IAdminThemeService>().GetAdminThemeAsync();

        await shapeTableManager.GetShapeTableAsync(themeId: null);
        await shapeTableManager.GetShapeTableAsync(siteTheme.Id);
        await shapeTableManager.GetShapeTableAsync(adminTheme.Id);

        return Ok();
    }
}
