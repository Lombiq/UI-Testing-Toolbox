using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
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
    public const string DuplicateCommandFailureCategory = "DuplicateCommandText";
    public const string DuplicateCommandWithParametersFailureCategory = "DuplicateCommandWithParameters";
    public const string ResultSetRowCountFailureCategory = "ResultSetRowCount";

    /// <summary>
    /// Holds threshold values for SQL query monitoring.
    /// </summary>
    public sealed record SqlQueryMonitoringThresholds(
        int? DuplicateCommandThreshold,
        int? DuplicateCommandWithParametersThreshold,
        int? ResultSetRowCountThreshold);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically run SQL query monitoring assertions every time a page
    /// changes (either due to explicit navigation or clicks). Disabled by default.
    /// </summary>
    public bool RunSqlQueryMonitoringAssertionOnAllPageChanges { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SQL query monitoring is enabled in the Orchard Core app for this test
    /// run. Disabled by default. When this is <see langword="false"/>, the app will not register the SQL monitoring
    /// services or middleware.
    /// </summary>
    public bool EnableSqlQueryMonitoringCollection { get; set; }

    /// <summary>
    /// Gets or sets a predicate that determines whether SQL query monitoring and asserting the results should run for
    /// the current page. This is only used if <see cref="RunSqlQueryMonitoringAssertionOnAllPageChanges"/> is set to
    /// <see langword="true"/>. Defaults to
    /// <see cref="EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule"/>.
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
    /// Gets or sets how long SQL monitoring assertion methods should wait for a matching request summary to appear in
    /// the in-memory summary store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This timeout is used by methods in <see cref="SqlQueryMonitoringUITestContextExtensions"/> to mitigate timing
    /// races right after navigation or request completion.
    /// </para>
    /// </remarks>
    public TimeSpan SummaryLookupTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the polling interval used while waiting for SQL monitoring summaries to appear in the summary
    /// store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Smaller values can reduce assertion latency but increase polling frequency.
    /// </para>
    /// </remarks>
    public TimeSpan SummaryLookupInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets how long to keep waiting for follow-up request summaries after the last captured summary when
    /// using follow-up-inclusive assertions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When no new summary arrives during this quiet period, follow-up collection ends even if
    /// <see cref="SummaryLookupTimeout"/> has not yet elapsed.
    /// </para>
    /// </remarks>
    public TimeSpan FollowUpSummaryQuietPeriod { get; set; } = TimeSpan.FromMilliseconds(300);

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
    /// Leave this <see langword="null"/> to disable this check. Defaults to 15, which matches the sample tests and
    /// keeps cache-miss detection active without breaking typical pages.
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

    public static Predicate<SqlQueryExecutionEntry> BuildIgnoreCommandTextPatternFilter(params string[] patterns)
    {
        if (patterns == null || patterns.Length == 0) return _ => true;

        var regexes = patterns
            .Select(pattern =>
                new Regex(
                    pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)))
            .ToArray();

        return entry => !regexes.Any(regex => regex.IsMatch(entry.CommandText));
    }

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;

    private Task AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) throw new InvalidOperationException("SQL query monitoring summary was not available.");

        var failures = new List<string>();
        var executions = GetFilteredExecutions(summary.Executions);

        var hasDuplicateCommandFailures = AddDuplicateCommandFailures(executions, failures);
        var hasDuplicateCommandWithParametersFailures = AddDuplicateCommandWithParametersFailures(executions, failures);
        var hasResultSetFailures = AddResultSetFailures(executions, failures);

        var failureMessage = BuildFailureMessage(
            summary,
            failures,
            hasDuplicateCommandFailures,
            hasDuplicateCommandWithParametersFailures,
            hasResultSetFailures);

        failures.ShouldBeEmpty(failureMessage);

        return Task.CompletedTask;
    }

    public void WriteSqlQueryMonitoringCounters(ITestOutputHelper testOutputHelper, SqlQueryMonitoringSummary summary)
    {
        if (testOutputHelper == null || summary == null) return;

        var executions = GetFilteredExecutions(summary.Executions);

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

        var triggeredFailureCategories = GetTriggeredFailureCategories(
            duplicateCommandGroups,
            duplicateCommandWithParametersGroups,
            oversizedRowCounts);

        var message =
            "SQL monitoring counters after page change:" + Environment.NewLine +
            $"- Request: {summary.RequestMethod} {summary.RequestPath}" + Environment.NewLine +
            $"- Executions: {executions.Count.ToTechnicalString()}" + Environment.NewLine +
            $"- Duplicate command groups: {duplicateCommandGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandGroups).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(DuplicateCommandThreshold)})" + Environment.NewLine +
            $"- Duplicate command+parameters groups: {duplicateCommandWithParametersGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandWithParametersGroups).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(DuplicateCommandWithParametersThreshold)})" + Environment.NewLine +
            $"- Result set rows observed: {oversizedRowCounts.Count.ToTechnicalString()} " +
            $"(max rows: {GetMaxOrZero(oversizedRowCounts).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(ResultSetRowCountThreshold)})" + Environment.NewLine +
            "- Triggered failure categories (at current thresholds): " +
            $"{(triggeredFailureCategories.Count == 0 ? "(none)" : string.Join(", ", triggeredFailureCategories))}";

        testOutputHelper.WriteLineTimestampedAndDebug(message);
    }

    private static int GetMaxOrZero(List<int> values) =>
        values.Count == 0 ? 0 : values.Max();

    private List<SqlQueryExecutionEntry> GetFilteredExecutions(IReadOnlyList<SqlQueryExecutionEntry> executions) =>
        executions.Where(entry => ExecutionFilter?.Invoke(entry) != false).ToList();

    private static string FormatThreshold(int? threshold) =>
        threshold?.ToTechnicalString() ?? "null";

    private bool AddDuplicateCommandFailures(
        List<SqlQueryExecutionEntry> executions,
        List<string> failures)
    {
        if (DuplicateCommandThreshold is not { } duplicateThreshold) return false;

        var duplicates = executions
            .GroupBy(entry => entry.NormalizedCommandText)
            .Where(group => group.Count() >= duplicateThreshold)
            .OrderByDescending(group => group.Count());

        var hasFailures = false;

        foreach (var group in duplicates)
        {
            var count = group.Count().ToTechnicalString();
            var threshold = duplicateThreshold.ToTechnicalString();
            hasFailures = true;
            failures.Add(
                $"[{DuplicateCommandFailureCategory}] Command text executed {count} times (threshold: {threshold}): " +
                $"{group.First().CommandText}" +
                FormatCallStackDetails(group));
        }

        return hasFailures;
    }

    private bool AddDuplicateCommandWithParametersFailures(
        List<SqlQueryExecutionEntry> executions,
        List<string> failures)
    {
        if (DuplicateCommandWithParametersThreshold is not { } duplicateWithParametersThreshold) return false;

        var duplicates = executions
            .GroupBy(entry => (entry.NormalizedCommandText, entry.ParameterSignature))
            .Where(group => group.Count() >= duplicateWithParametersThreshold)
            .OrderByDescending(group => group.Count());

        var hasFailures = false;

        foreach (var group in duplicates)
        {
            var sample = group.First();
            var count = group.Count().ToTechnicalString();
            var threshold = duplicateWithParametersThreshold.ToTechnicalString();
            hasFailures = true;
            failures.Add($"[{DuplicateCommandWithParametersFailureCategory}] " +
                $"Command text with same parameters executed {count} times (threshold: {threshold}):" +
                $" {sample.CommandText} [{sample.ParameterSignature}]" +
                FormatCallStackDetails(group));
        }

        return hasFailures;
    }

    private bool AddResultSetFailures(
        List<SqlQueryExecutionEntry> executions,
        List<string> failures)
    {
        if (ResultSetRowCountThreshold is not { } resultSetThreshold) return false;

        var oversizedResults = executions
            .Where(entry => entry.RowCount is > 0 && entry.RowCount > resultSetThreshold)
            .OrderByDescending(entry => entry.RowCount);

        var hasFailures = false;

        foreach (var entry in oversizedResults)
        {
            var threshold = resultSetThreshold.ToTechnicalString();
            var rowCount = entry.RowCount.ToTechnicalString();
            hasFailures = true;
            failures.Add(
                $"[{ResultSetRowCountFailureCategory}] Command result set had {rowCount} rows (threshold: {threshold}): " +
                entry.CommandText +
                FormatCallStackDetails([entry]));
        }

        return hasFailures;
    }

    private static string FormatCallStackDetails(IEnumerable<SqlQueryExecutionEntry> entries)
    {
        var callStacks = entries
            .Select(entry => entry.CallStack)
            .Where(callStack => !string.IsNullOrWhiteSpace(callStack))
            .ToList();

        if (callStacks.Count == 0) return string.Empty;

        return Environment.NewLine +
            "Call stacks:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine + Environment.NewLine,
                callStacks.Select((callStack, index) =>
                    $"{(index + 1).ToTechnicalString()}.{Environment.NewLine}{Indent(callStack, "   ")}"));
    }

    private static string Indent(string text, string indentationPrefix)
    {
        var normalized = text.ReplaceOrdinalIgnoreCase("\r\n", "\n");
        return string.Join(
            Environment.NewLine,
            normalized
                .Split('\n')
                .Select(line => indentationPrefix + line));
    }

    private string BuildFailureMessage(
        SqlQueryMonitoringSummary summary,
        List<string> failures,
        bool hasDuplicateCommandFailures,
        bool hasDuplicateCommandWithParametersFailures,
        bool hasResultSetFailures)
    {
        if (failures.Count == 0) return null;

        var header =
            $"SQL query monitoring detected potential performance issues on {summary.RequestMethod} {summary.RequestPath}.";

        var categoryGuide = new List<string>();
        var triggeredCategories = new List<string>();
        var configuredThresholds = new List<string>();
        void AddFailureCategoryDetails(
            bool shouldAdd,
            string category,
            string categoryDescription,
            string thresholdName,
            int? thresholdValue)
        {
            if (!shouldAdd) return;

            categoryGuide.Add($"[{category}] {categoryDescription}");
            triggeredCategories.Add(category);
            configuredThresholds.Add($"{thresholdName}={FormatThreshold(thresholdValue)}");
        }

        AddFailureCategoryDetails(
            hasDuplicateCommandFailures,
            DuplicateCommandFailureCategory,
            "The same database query text was executed more times than the configured threshold allows. " +
            "This can indicate a SELECT N+1 problem or repeated queries that should be consolidated.",
            nameof(DuplicateCommandThreshold),
            DuplicateCommandThreshold);
        AddFailureCategoryDetails(
            hasDuplicateCommandWithParametersFailures,
            DuplicateCommandWithParametersFailureCategory,
            "The same database query (with identical parameters) was executed repeatedly. " +
            "This can indicate missing caching or repeated queries from multiple call sites.",
            nameof(DuplicateCommandWithParametersThreshold),
            DuplicateCommandWithParametersThreshold);
        AddFailureCategoryDetails(
            hasResultSetFailures,
            ResultSetRowCountFailureCategory,
            "Some queries returned more rows than the configured threshold allows. " +
            "This can indicate missing filtering in SQL and work being done in application code.",
            nameof(ResultSetRowCountThreshold),
            ResultSetRowCountThreshold);

        return
            header + Environment.NewLine +
            "Triggered failure categories:" + Environment.NewLine +
            string.Join(Environment.NewLine, triggeredCategories.Select((item, index) => $"{(index + 1).ToTechnicalString()}. {item}")) +
            Environment.NewLine +
            "Category guide:" + Environment.NewLine +
            string.Join(Environment.NewLine, categoryGuide.Select((item, index) => $"{(index + 1).ToTechnicalString()}. {item}")) +
            Environment.NewLine +
            "Threshold configuration:" + Environment.NewLine +
            string.Join(Environment.NewLine, configuredThresholds.Select((item, index) => $"{(index + 1).ToTechnicalString()}. {item}")) +
            Environment.NewLine +
            "See the details below:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures);
    }

    private List<string> GetTriggeredFailureCategories(
        List<int> duplicateCommandGroups,
        List<int> duplicateCommandWithParametersGroups,
        List<int> rowCounts)
    {
        var categories = new List<string>();

        if (DuplicateCommandThreshold is { } duplicateCommandThreshold &&
            duplicateCommandGroups.Any(groupSize => groupSize >= duplicateCommandThreshold))
        {
            categories.Add(DuplicateCommandFailureCategory);
        }

        if (DuplicateCommandWithParametersThreshold is { } duplicateWithParametersThreshold &&
            duplicateCommandWithParametersGroups.Any(groupSize => groupSize >= duplicateWithParametersThreshold))
        {
            categories.Add(DuplicateCommandWithParametersFailureCategory);
        }

        if (ResultSetRowCountThreshold is { } resultSetThreshold &&
            rowCounts.Any(rowCount => rowCount > resultSetThreshold))
        {
            categories.Add(ResultSetRowCountFailureCategory);
        }

        return categories;
    }
}
