using Lombiq.HelpfulLibraries.AspNetCore.Extensions;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.YesSql;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInteractiveModeStatusAccessor, InteractiveModeStatusAccessor>();
        services.AddAsyncResultFilter<ApplicationInfoInjectingFilter>();

        // To ensure we don't encounter any concurrency issue, enable EnableThreadSafetyChecks for all tests.
        services.Configure<YesSqlOptions>(options => options.EnableThreadSafetyChecks = true);
    }
}
