using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringMiddleware
{
    private readonly RequestDelegate _next;

    public SqlQueryMonitoringMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context)
    {
        EnsureConnectionFactoryWrapped(context);

        try
        {
            await _next(context);
        }
        finally
        {
            var services = context.RequestServices;
            var monitoringContext = services.GetService<ISqlQueryMonitoringContext>();
            var store = services.GetService<ISqlQueryMonitoringStore>();

            if (monitoringContext != null && store != null)
            {
                var summary = new SqlQueryMonitoringSummary(
                    requestPath: $"{context.Request.Path}{context.Request.QueryString}",
                    requestMethod: context.Request.Method,
                    traceIdentifier: context.TraceIdentifier,
                    completedUtc: DateTimeOffset.UtcNow,
                    executions: monitoringContext.Executions);

                store.AddSummary(summary);
            }
        }
    }

    private static void EnsureConnectionFactoryWrapped(HttpContext context)
    {
        var store = context.RequestServices.GetService<IStore>();
        if (store?.Configuration?.ConnectionFactory == null) return;

        if (store.Configuration.ConnectionFactory is SqlQueryMonitoringConnectionFactory) return;

        var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
        store.Configuration.ConnectionFactory =
            new SqlQueryMonitoringConnectionFactory(store.Configuration.ConnectionFactory, httpContextAccessor);
    }
}
