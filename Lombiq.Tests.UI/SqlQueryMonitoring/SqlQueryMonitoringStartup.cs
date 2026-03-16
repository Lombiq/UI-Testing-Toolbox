using Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using System;
using YesSql;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringStartup : StartupBase
{
    // We set the order so the startup runs before the YesSql configuration that uses the connection factory. This lets
    // us wrap IStore.Configuration.ConnectionFactory early enough so all DB commands are intercepted. If the startup
    // runs later, other components may already have captured the unwrapped connection factory, and our monitoring won’t
    // see SQL at all (or will see it inconsistently).
    public override int Order => -500;

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        app.UseMiddleware<SqlQueryMonitoringMiddleware>();

        // We wrap here too, not only in the middleware.
        // The middleware runs on requests, but some code may get the connection factory before the first request.
        // Wrapping here covers those early cases. The middleware still checks again on each request.
        var store = serviceProvider.GetService<IStore>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        SqlQueryMonitoringConnectionFactoryHelper.EnsureWrapped(store, httpContextAccessor);
    }
}
