using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInteractiveModeStatusAccessor, InteractiveModeStatusAccessor>();
        services.AddAsyncResultFilter<ApplicationInfoInjectingFilter>();
    }
}
