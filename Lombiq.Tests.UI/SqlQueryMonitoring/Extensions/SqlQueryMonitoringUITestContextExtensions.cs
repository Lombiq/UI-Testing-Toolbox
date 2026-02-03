using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;

public static class SqlQueryMonitoringUITestContextExtensions
{
    /// <summary>
    /// Executes assertions on the SQL query monitoring summary recorded for the most recent request.
    /// </summary>
    /// <param name="assertSqlQueryMonitoringSummaryAsync">
    /// The assertion logic to run on the monitoring summary. If <see langword="null"/> then the assertion supplied in
    /// the context will be used.
    /// </param>
    public static async Task AssertSqlQueryMonitoringAsync(
        this UITestContext context,
        Func<SqlQueryMonitoringSummary, Task> assertSqlQueryMonitoringSummaryAsync = null)
    {
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
        var configuration = context.Configuration.SqlQueryMonitoringConfiguration;
        configuration.WriteSqlQueryMonitoringCounters(context.Configuration.TestOutputHelper, summary);

        try
        {
            var assertTask = (assertSqlQueryMonitoringSummaryAsync ?? configuration.AssertSqlQueryMonitoringSummaryAsync)?
                .Invoke(summary);
            await (assertTask ?? Task.CompletedTask);
        }
        catch (Exception exception)
        {
            throw new SqlQueryMonitoringAssertionException(summary, configuration, exception);
        }
    }

    /// <summary>
    /// Returns the SQL query monitoring summary recorded for the most recent request.
    /// </summary>
    private static Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(this UITestContext context) =>
        GetLatestSqlQueryMonitoringSummaryAsync(context, tenant: context.TenantName);

    private static async Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(
        this UITestContext context,
        string tenant)
    {
        var store = context.Application.Services.GetService<ISqlQueryMonitoringStore>();

        if (store == null)
        {
            await context.Application.UsingScopeAsync(
                serviceProvider =>
                {
                    store = serviceProvider.GetService<ISqlQueryMonitoringStore>();
                    return Task.CompletedTask;
                },
                tenant);
        }

        if (store == null || !store.TryDequeueMostRecentWithExecutions(out var summary))
        {
            throw new InvalidOperationException(
                "No SQL query monitoring summary was captured. Ensure the page has finished loading, executes SQL " +
                "commands, and that SQL query monitoring is enabled.");
        }

        return summary;
    }
}
