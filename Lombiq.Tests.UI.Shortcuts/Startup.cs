using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OrchardCore.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Shortcuts;

public class Startup : StartupBase
{
    public override int Order => int.MaxValue;
    public override int ConfigureOrder => int.MaxValue;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<MvcOptions>(options => options.Filters.Add(typeof(ApplicationInfoInjectingFilter)));

        services.Configure<AuthenticationOptions>(options =>
        {
            const string scheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            if (options.Schemes.All(x => x.Name != scheme)) return;

            (options.Schemes as IList<AuthenticationSchemeBuilder>).RemoveAll(x => x.Name == scheme);
            options.SchemeMap.Remove(scheme);

            options.AddScheme<FakeOpenIddictValidationAspNetCoreHandler>(scheme, displayName: null);
        });
    }
}
