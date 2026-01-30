using Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;
using Lombiq.Tests.UI.Helpers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public class SqlQueryMonitoringConfiguration
{
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
    /// this <see langword="null"/> to disable this check.
    /// </summary>
    public int? DuplicateCommandThreshold { get; set; }

    /// <summary>
    /// Gets or sets the threshold for how many times the same SQL command text can be executed with the same parameter
    /// values in a single request. If the count is greater than or equal to this threshold, the assertion fails.
    /// Leave this <see langword="null"/> to disable this check.
    /// </summary>
    public int? DuplicateCommandWithParametersThreshold { get; set; }

    /// <summary>
    /// Gets or sets the threshold for how many rows a SQL command can return in a single request. If the count is
    /// greater than this threshold, the assertion fails. Leave this <see langword="null"/> to disable this check.
    /// </summary>
    public int? ResultSetRowCountThreshold { get; set; }

    public SqlQueryMonitoringConfiguration() =>
        AssertSqlQueryMonitoringSummaryAsync = AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync;

    public Task AssertSqlQueryMonitoringSummaryAgainstThresholdsAsync(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) throw new InvalidOperationException("SQL query monitoring summary was not available.");

        var failures = new List<string>();
        var executions = summary.Executions.Where(entry => ExecutionFilter?.Invoke(entry) != false).ToList();

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
                failures.Add($"Command text with parameters executed {count} times (threshold: {threshold}):" +
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
                failures.Add(
                        $"Command result set had {rowCount} rows (threshold: {threshold}): " + ShortenCommandText(entry.CommandText));
            }
        }

        string failureMessage = null;
        if (failures.Count != 0)
        {
            failureMessage =
                $"SQL query monitoring detected potential issues on {summary.RequestMethod} {summary.RequestPath}:{Environment.NewLine}" +
                string.Join(Environment.NewLine, failures);
        }

        failures.ShouldBeEmpty(failureMessage);

        return Task.CompletedTask;
    }

    private static string ShortenCommandText(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText)) return "(empty command)";

        return commandText.Length <= 300
            ? commandText
            : commandText[..300] + "...";
    }

    public static Predicate<SqlQueryExecutionEntry> BuildIgnoreCommandTextPatternFilter(params string[] patterns)
    {
        if (patterns == null || patterns.Length == 0) return _ => true;

        var regexes = patterns.Select(pattern =>
            new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

        return entry => !regexes.Any(regex => regex.IsMatch(entry.CommandText));
    }

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesSqlQueryMonitoringAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;
}
