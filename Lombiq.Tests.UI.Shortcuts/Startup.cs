using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using System;

namespace Lombiq.Tests.UI.Shortcuts;

public class Startup : StartupBase
{
    public override int Order => -100;
    public override int ConfigureOrder => -100;

    public override void ConfigureServices(IServiceCollection services) =>
        services.Configure<MvcOptions>((options) => options.Filters.Add(typeof(ApplicationInfoInjectingFilter)));

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        routes.MapAreaControllerRoute(
            name: "TenantsApiCreateProxy",
            areaName: "Lombiq.Tests.UI.Shortcuts",
            pattern: "api/tenants/create",
            defaults: new
            {
                controller = typeof(TenantCreateApiProxyController).ControllerName(),
                action = nameof(TenantCreateApiProxyController.Create),
            }
        );
}
