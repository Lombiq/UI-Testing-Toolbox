using Lombiq.Tests.UI.AdminTheme.Constants;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.AdminTheme;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddResourceFilter(
            builder => builder
                .Always()
                .RegisterStylesheet(ResourceNames.General),
            FeatureIds.Area);

        services.AddResourceManagementConfiguration<ResourceManagementOptionsConfiguration>();
    }
}
