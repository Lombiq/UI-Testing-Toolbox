using Lombiq.HelpfulLibraries.AspNetCore.Extensions;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.Shortcuts;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInteractiveModeStatusAccessor, InteractiveModeStatusAccessor>();
        services.AddAsyncResultFilter<ApplicationInfoInjectingFilter>();
        services.AddScoped<IModularTenantEvents, CdnDisabler>();
    }
}

[Feature("Lombiq.Tests.UI.Shortcuts.Swagger")]
public class SwaggerStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services) =>
        services.AddSwaggerGen(swaggerGenOptions =>
            swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo { Title = "Orchard Core API", Version = "v1" }));

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        app.UseSwagger();
}
