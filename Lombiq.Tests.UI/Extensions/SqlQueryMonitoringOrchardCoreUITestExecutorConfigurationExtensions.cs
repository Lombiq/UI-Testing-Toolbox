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
    /// Configures SQL query monitoring thresholds per page based on the target URL, using regular expressions.
    /// </summary>
    /// <param name="configuration">The test configuration to attach the rule to.</param>
    /// <param name="defaultThresholds">
    /// The thresholds to apply when no regex rule matches the requested URL.
    /// </param>
    /// <param name="rules">
    /// A list of regex pattern/threshold pairs. The first matching pattern wins. The regex is matched against the
    /// request path (e.g. <c>/categories/travel</c>).
    /// </param>
    /// <remarks>
    /// <para>
    /// This attaches a handler to <see cref="UITestExecutionEvents.BeforeNavigation"/> and updates the SQL monitoring
    /// thresholds for the upcoming page change. Use this when you want to tune limits per feature or page.
    /// </para>
    /// <para>
    /// The regex matching is case-insensitive and uses a 1-second timeout to prevent pathological patterns.
    /// </para>
    /// </remarks>
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
            ?? [];

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
