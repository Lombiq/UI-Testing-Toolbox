using Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;
using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Configures SQL query monitoring thresholds based on the target URL, using regular expressions.
    /// </summary>
    public static void ConfigureSqlQueryMonitoringThresholdsForPages(
        this OrchardCoreUITestExecutorConfiguration configuration,
        SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds defaultThresholds,
        params (string Pattern, SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds Thresholds)[] rules)
    {
        var compiledRules = rules?
            .Select(rule => (Regex: new Regex(
                    rule.Pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)),
                rule.Thresholds))
            .ToList()
            ?? new List<(Regex Regex, SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds Thresholds)>();

        configuration.Events.BeforeNavigation += (_, targetUri) =>
        {
            var thresholds = defaultThresholds;

            foreach (var rule in compiledRules)
            {
                if (rule.Regex.IsMatch(targetUri.AbsolutePath))
                {
                    thresholds = rule.Thresholds;
                    break;
                }
            }

            configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = thresholds.DuplicateCommandThreshold;
            configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold =
                thresholds.DuplicateCommandWithParametersThreshold;
            configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = thresholds.ResultSetRowCountThreshold;

            return Task.CompletedTask;
        };
    }
}
