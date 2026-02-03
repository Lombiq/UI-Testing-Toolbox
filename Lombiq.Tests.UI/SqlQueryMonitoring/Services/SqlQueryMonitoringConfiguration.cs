using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Services;

public class SqlQueryMonitoringConfiguration
{
    /// <summary>
    /// Holds threshold values for SQL query monitoring.
    /// </summary>
    public sealed record SqlQueryMonitoringThresholds(
        int? DuplicateCommandThreshold,
        int? DuplicateCommandWithParametersThreshold,
        int? ResultSetRowCountThreshold);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically run SQL query monitoring assertions every time a page
    /// changes (either due to explicit navigation or clicks).
    /// </summary>
    public bool RunSqlQueryMonitoringAssertionOnAllPageChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets a predicate that determines whether SQL query monitoring and asserting the results should run for
    /// the current page. This is only used if <see cref="RunSqlQueryMonitoringAssertionOnAllPageChanges"/> is set to
    /// <see langword="true"/>. Defaults to <see cref="EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule"/>.
    /// </summary>
    public Predicate<UITestContext> SqlQueryMonitoringAndAssertionOnPageChangeRule { get; set; } =
        EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule;

    /// <summary>
    /// Gets or sets a delegate to run assertions on the SQL query monitoring summary. Defaults to <see
    /// cref="AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync"/>.
    /// </summary>
    public Func<SqlQueryMonitoringSummary, Task> AssertSqlQueryMonitoringSummaryAsync { get; set; }

    /// <summary>
    /// Gets or sets a predicate that filters out SQL command executions from monitoring. Defaults to allowing all
    /// executions.
    /// </summary>
    public Predicate<SqlQueryExecutionEntry> ExecutionFilter { get; set; } = _ => true;

    /// <summary>
    /// Gets or sets the threshold for how many times the same SQL command text can be executed in a single request,
    /// regardless of parameters. If the count is greater than or equal to this threshold, the assertion fails. Leave
    /// this <see langword="null"/> to disable this check. Defaults to 30, which is intentionally conservative to avoid
    /// false positives on typical Orchard Core pages while still flagging obvious N+1 patterns.
    /// </summary>
    public int? DuplicateCommandThreshold { get; set; } = 30;

    /// <summary>
    /// Gets or sets the threshold for how many times the same SQL command text can be executed with the same parameter
    /// values in a single request. If the count is greater than or equal to this threshold, the assertion fails.
    /// Leave this <see langword="null"/> to disable this check. Defaults to 15, which matches the sample tests and keeps
    /// cache-miss detection active without breaking typical pages.
    /// </summary>
    public int? DuplicateCommandWithParametersThreshold { get; set; } = 15;

    /// <summary>
    /// Gets or sets the threshold for how many rows a SQL command can return in a single request. If the count is
    /// greater than this threshold, the assertion fails. Leave this <see langword="null"/> to disable this check.
    /// Defaults to 200, chosen to tolerate common list queries while still surfacing unbounded result sets.
    /// </summary>
    public int? ResultSetRowCountThreshold { get; set; } = 200;

    public SqlQueryMonitoringConfiguration() =>
        AssertSqlQueryMonitoringSummaryAsync = AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync;

    private Task AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) throw new InvalidOperationException("SQL query monitoring summary was not available.");

        var failures = new List<string>();
        var executions = summary.Executions.Where(entry => ExecutionFilter?.Invoke(entry) != false).ToList();

        var hasDuplicateCommandFailures = false;
        var hasDuplicateCommandWithParametersFailures = false;
        var hasResultSetFailures = false;

        if (DuplicateCommandThreshold is { } duplicateThreshold)
        {
            var duplicates = executions
                .GroupBy(entry => entry.NormalizedCommandText)
                .Where(group => group.Count() >= duplicateThreshold)
                .OrderByDescending(group => group.Count());

            foreach (var group in duplicates)
            {
                var count = group.Count().ToTechnicalString();
                var threshold = duplicateThreshold.ToTechnicalString();
                hasDuplicateCommandFailures = true;
                failures.Add(
                    $"Command text executed {count} times (threshold: {threshold}): {ShortenCommandText(group.First().CommandText)}");
            }
        }

        if (DuplicateCommandWithParametersThreshold is { } duplicateWithParametersThreshold)
        {
            var duplicates = executions
                .GroupBy(entry => (entry.NormalizedCommandText, entry.ParameterSignature))
                .Where(group => group.Count() >= duplicateWithParametersThreshold)
                .OrderByDescending(group => group.Count());

            foreach (var group in duplicates)
            {
                var sample = group.First();
                var count = group.Count().ToTechnicalString();
                var threshold = duplicateWithParametersThreshold.ToTechnicalString();
                hasDuplicateCommandWithParametersFailures = true;
                failures.Add($"Command text with same parameters executed {count} times (threshold: {threshold}):" +
                    $" {ShortenCommandText(sample.CommandText)} [{sample.ParameterSignature}]");
            }
        }

        if (ResultSetRowCountThreshold is { } resultSetThreshold)
        {
            var oversizedResults = executions
                .Where(entry => entry.RowCount is > 0 && entry.RowCount > resultSetThreshold)
                .OrderByDescending(entry => entry.RowCount);

            foreach (var entry in oversizedResults)
            {
                var threshold = resultSetThreshold.ToTechnicalString();
                var rowCount = entry.RowCount.ToTechnicalString();
                hasResultSetFailures = true;
                failures.Add(
                        $"Command result set had {rowCount} rows (threshold: {threshold}): " + ShortenCommandText(entry.CommandText));
            }
        }

        string failureMessage = null;
        if (failures.Count != 0)
        {
            var header =
                $"SQL query monitoring detected potential performance issues on {summary.RequestMethod} {summary.RequestPath}.";

            var guidance = new List<string>();
            var configuredThresholds = new List<string>();

            if (hasDuplicateCommandFailures)
            {
                guidance.Add(
                    "The same database query text was executed more times than the configured threshold allows. " +
                    "This can indicate a SELECT N+1 problem or repeated queries that should be consolidated.");
                configuredThresholds.Add(
                    $"{nameof(DuplicateCommandThreshold)}={DuplicateCommandThreshold?.ToTechnicalString() ?? "null"}");
            }

            if (hasDuplicateCommandWithParametersFailures)
            {
                guidance.Add(
                    "The same database query (with identical parameters) was executed repeatedly. " +
                    "This can indicate missing caching or repeated queries from multiple call sites.");
                configuredThresholds.Add(
                    $"{nameof(DuplicateCommandWithParametersThreshold)}=" +
                    $"{DuplicateCommandWithParametersThreshold?.ToTechnicalString() ?? "null"}");
            }

            if (hasResultSetFailures)
            {
                guidance.Add(
                    "Some queries returned more rows than the configured threshold allows. " +
                    "This can indicate missing filtering in SQL and work being done in application code.");
                configuredThresholds.Add(
                    $"{nameof(ResultSetRowCountThreshold)}={ResultSetRowCountThreshold?.ToTechnicalString() ?? "null"}");
            }

            failureMessage =
                header + Environment.NewLine +
                "Triggered checks:" + Environment.NewLine +
                string.Join(Environment.NewLine, guidance.Select((item, index) => $"{(index + 1).ToTechnicalString()}. {item}")) +
                Environment.NewLine +
                "Threshold configuration:" + Environment.NewLine +
                string.Join(Environment.NewLine, configuredThresholds.Select((item, index) => $"{(index + 1).ToTechnicalString()}. {item}")) +
                Environment.NewLine +
                "See the details below:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures);
        }

        failures.ShouldBeEmpty(failureMessage);

        return Task.CompletedTask;
    }

    public void WriteSqlQueryMonitoringCounters(ITestOutputHelper testOutputHelper, SqlQueryMonitoringSummary summary)
    {
        if (testOutputHelper == null || summary == null) return;

        var executions = summary.Executions.Where(entry => ExecutionFilter?.Invoke(entry) != false).ToList();

        var duplicateCommandGroups = executions
            .GroupBy(entry => entry.NormalizedCommandText)
            .Select(group => group.Count())
            .ToList();

        var duplicateCommandWithParametersGroups = executions
            .GroupBy(entry => (entry.NormalizedCommandText, entry.ParameterSignature))
            .Select(group => group.Count())
            .ToList();

        var oversizedRowCounts = executions
            .Where(entry => entry.RowCount is > 0)
            .Select(entry => entry.RowCount.Value)
            .ToList();

        var message =
            "SQL monitoring counters after page change:" + Environment.NewLine +
            $"- Request: {summary.RequestMethod} {summary.RequestPath}" + Environment.NewLine +
            $"- Executions: {executions.Count.ToTechnicalString()}" + Environment.NewLine +
            $"- Duplicate command groups: {duplicateCommandGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandGroups).ToTechnicalString()}, " +
            $"threshold: {DuplicateCommandThreshold?.ToTechnicalString() ?? "null"})" + Environment.NewLine +
            $"- Duplicate command+parameters groups: {duplicateCommandWithParametersGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandWithParametersGroups).ToTechnicalString()}, " +
            $"threshold: {DuplicateCommandWithParametersThreshold?.ToTechnicalString() ?? "null"})" + Environment.NewLine +
            $"- Result set rows observed: {oversizedRowCounts.Count.ToTechnicalString()} " +
            $"(max rows: {GetMaxOrZero(oversizedRowCounts).ToTechnicalString()}, " +
            $"threshold: {ResultSetRowCountThreshold?.ToTechnicalString() ?? "null"})";

        testOutputHelper.WriteLineTimestampedAndDebug(message);
    }

    private static string ShortenCommandText(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText)) return "(empty command)";

        return commandText.Length <= 300
            ? commandText
            : commandText[..300] + "...";
    }

    private static int GetMaxOrZero(List<int> values) =>
        values.Count == 0 ? 0 : values.Max();

    public static Predicate<SqlQueryExecutionEntry> BuildIgnoreCommandTextPatternFilter(params string[] patterns)
    {
        if (patterns == null || patterns.Length == 0) return _ => true;

        var regexes = patterns.Select(pattern =>
            new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1)))
            .ToArray();

        return entry => !regexes.Any(regex => regex.IsMatch(entry.CommandText));
    }

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;
}
