using Lombiq.HelpfulLibraries.AspNetCore.Extensions;
using Lombiq.Tests.UI.Shortcuts.Middlewares;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using OrchardCore.Data.YesSql;
using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.Shortcuts;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInteractiveModeStatusAccessor, InteractiveModeStatusAccessor>();
        services.AddAsyncResultFilter<ApplicationInfoInjectingFilter>();
        services.AddScoped<IModularTenantEvents, CdnDisabler>();

        // To ensure we don't encounter any concurrency issue, enable EnableThreadSafetyChecks for all tests.
        services.Configure<YesSqlOptions>(options => options.EnableThreadSafetyChecks = true);
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        app.UseMiddleware<ExceptionContextLoggingMiddleware>();
}

[Feature(ShortcutsFeatureIds.OpenApi)]
public sealed class OpenApiSetup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services) =>
        services.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        routes.MapOpenApi();
}

[Feature(ShortcutsFeatureIds.ShiftTime)]
public sealed class SetTimeStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveImplementationsOf<IClock>();
        services.AddSingleton<IClock, TimeShiftingClock>();
    }
}
