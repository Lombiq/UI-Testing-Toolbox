using Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class SqlQueryMonitoringOrchardCoreUITestExecutorConfigurationExtensions
{
    /// <summary>
    /// Sets up SQL query monitoring to run every time a page changes (either due to explicit navigation or clicks) and
    /// asserts on the monitoring results.
    /// </summary>
    /// <param name="assertSqlQueryMonitoringSummaryAsync">
    /// The assertion logic to run on the SQL query monitoring summary. If <see langword="null"/> then the assertion
    /// supplied in the context will be used.
    /// </param>
    public static void SetUpSqlQueryMonitoringAssertionOnPageChange(
        this OrchardCoreUITestExecutorConfiguration configuration,
        Func<SqlQueryMonitoringSummary, Task> assertSqlQueryMonitoringSummaryAsync = null)
    {
        if (!configuration.CustomConfiguration.TryAdd("SqlQueryMonitoringAssertionOnPageChangeWasSetUp", value: true)) return;

        configuration.Events.AfterPageChange += async context =>
        {
            if (configuration.SqlQueryMonitoringConfiguration.SqlQueryMonitoringAndAssertionOnPageChangeRule?.Invoke(context) == true)
            {
                await context.AssertSqlQueryMonitoringAsync(assertSqlQueryMonitoringSummaryAsync);
            }
        };
    }
}
