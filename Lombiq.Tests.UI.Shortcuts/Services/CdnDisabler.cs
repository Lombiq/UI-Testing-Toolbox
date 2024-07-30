using Microsoft.Extensions.Configuration;
using OrchardCore.Modules;
using OrchardCore.Settings;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

internal sealed class CdnDisabler : ModularTenantEvents
{
    private readonly ISiteService _siteService;
    private readonly IConfiguration _shellConfiguration;

    public CdnDisabler(ISiteService siteService, IConfiguration shellConfiguration)
    {
        _siteService = siteService;
        _shellConfiguration = shellConfiguration;
    }

    public override async Task ActivatedAsync()
    {
        if (_shellConfiguration.GetValue<bool>("Lombiq_Tests_UI:DontDisableUseCdn"))
        {
            return;
        }

        var site = await _siteService.LoadSiteSettingsAsync();
        site.UseCdn = false;
        await _siteService.UpdateSiteSettingsAsync(site);
    }
}
