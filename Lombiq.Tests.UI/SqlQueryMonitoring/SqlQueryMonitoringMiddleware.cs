using Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;
using System;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

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
                var tenantName = services.GetService<ShellSettings>()?.Name ?? ShellSettings.DefaultShellName;
                var summary = new SqlQueryMonitoringSummary(
                    tenantName: tenantName,
                    requestPath: $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
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
        var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
        SqlQueryMonitoringConnectionFactoryHelper.EnsureWrapped(store, httpContextAccessor);
    }
}
