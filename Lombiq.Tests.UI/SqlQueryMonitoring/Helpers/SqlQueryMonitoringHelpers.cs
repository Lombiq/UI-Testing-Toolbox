using Lombiq.Tests.UI.SqlQueryMonitoring.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;

public static class SqlQueryMonitoringHelpers
{
    public static void WriteSqlQueryMonitoringCounters(
        ITestOutputHelper testOutputHelper,
        SqlQueryMonitoringSummary summary,
        SqlQueryMonitoringConfiguration configuration)
    {
        if (testOutputHelper == null || summary == null) return;

        var executions = GetFilteredExecutions(summary.Executions, configuration);

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
            configuration,
            duplicateCommandGroups,
            duplicateCommandWithParametersGroups,
            oversizedRowCounts);

        var message =
            "SQL monitoring counters after page change:" + Environment.NewLine +
            $"- Request: {summary.RequestMethod} {summary.RequestPath}" + Environment.NewLine +
            $"- Executions: {executions.Count.ToTechnicalString()}" + Environment.NewLine +
            $"- Duplicate command groups: {duplicateCommandGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandGroups).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(configuration.DuplicateCommandThreshold)})" + Environment.NewLine +
            $"- Duplicate command+parameters groups: {duplicateCommandWithParametersGroups.Count.ToTechnicalString()} " +
            $"(max group size: {GetMaxOrZero(duplicateCommandWithParametersGroups).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(configuration.DuplicateCommandWithParametersThreshold)})" + Environment.NewLine +
            $"- Result set rows observed: {oversizedRowCounts.Count.ToTechnicalString()} " +
            $"(max rows: {GetMaxOrZero(oversizedRowCounts).ToTechnicalString()}, " +
            $"threshold: {FormatThreshold(configuration.ResultSetRowCountThreshold)})" + Environment.NewLine +
            "- Triggered failure categories (at current thresholds): " +
            $"{(triggeredFailureCategories.Count == 0 ? "(none)" : string.Join(", ", triggeredFailureCategories))}";

        testOutputHelper.WriteLineTimestampedAndDebug(message);
    }

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

    public static IList<SqlQueryExecutionEntry> GetFilteredExecutions(
        IReadOnlyList<SqlQueryExecutionEntry> executions,
        SqlQueryMonitoringConfiguration configuration) =>
        executions.Where(entry => configuration.ExecutionFilter?.Invoke(entry) != false).ToList();

    public static string FormatThreshold(int? threshold) =>
        threshold?.ToTechnicalString() ?? "null";

    private static int GetMaxOrZero(List<int> values) =>
        values.Count == 0 ? 0 : values.Max();

    private static List<string> GetTriggeredFailureCategories(
        SqlQueryMonitoringConfiguration configuration,
        List<int> duplicateCommandGroups,
        List<int> duplicateCommandWithParametersGroups,
        List<int> rowCounts)
    {
        var categories = new List<string>();

        if (configuration.DuplicateCommandThreshold is { } duplicateCommandThreshold &&
            duplicateCommandGroups.Any(groupSize => groupSize >= duplicateCommandThreshold))
        {
            categories.Add(SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
        }

        if (configuration.DuplicateCommandWithParametersThreshold is { } duplicateWithParametersThreshold &&
            duplicateCommandWithParametersGroups.Any(groupSize => groupSize >= duplicateWithParametersThreshold))
        {
            categories.Add(SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory);
        }

        if (configuration.ResultSetRowCountThreshold is { } resultSetThreshold &&
            rowCounts.Any(rowCount => rowCount > resultSetThreshold))
        {
            categories.Add(SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory);
        }

        return categories;
    }
}
