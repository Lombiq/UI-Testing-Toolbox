using Lombiq.HelpfulLibraries.OrchardCore.ResourceManagement;
using Lombiq.Tests.UI.AdminTheme.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.AdminTheme;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddResourceManagementConfiguration<ResourceManagementOptionsConfiguration>();
        services.AddResourceFilter(
            builder => builder
                .Always()
                .RegisterStylesheet(ResourceNames.General),
            FeatureIds.AdminTheme);
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        app.UseResourceFilters();
}
