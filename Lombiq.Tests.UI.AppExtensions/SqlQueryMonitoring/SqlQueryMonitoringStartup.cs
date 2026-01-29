using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using System;
using YesSql;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringStartup : IStartup
{
    public int Order => -500;
    public int ConfigureOrder => -500;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        builder.UseMiddleware<SqlQueryMonitoringMiddleware>();

        var store = serviceProvider.GetService<IStore>();
        if (store?.Configuration?.ConnectionFactory == null) return;

        if (store.Configuration.ConnectionFactory is SqlQueryMonitoringConnectionFactory) return;

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        store.Configuration.ConnectionFactory =
            new SqlQueryMonitoringConnectionFactory(store.Configuration.ConnectionFactory, httpContextAccessor);
    }
}
