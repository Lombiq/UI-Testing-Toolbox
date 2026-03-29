using System;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

public sealed partial class SqlQueryExecutionEntry
{
    public string CommandText { get; }
    public string NormalizedCommandText { get; }
    public string ParameterSignature { get; }
    public int? RowCount { get; }
    public string CallStack { get; }

    public SqlQueryExecutionEntry(
        string commandText,
        string normalizedCommandText,
        string parameterSignature,
        int? rowCount,
        string callStack)
    {
        CommandText = commandText;
        NormalizedCommandText = normalizedCommandText;
        ParameterSignature = parameterSignature;
        RowCount = rowCount;
        CallStack = callStack;
    }

    public static SqlQueryExecutionEntry FromCommand(DbCommand command, int? rowCount)
    {
        var commandText = NormalizeCommandText(command.CommandText);
        // Use a stable form for grouping so formatting differences don't hide duplicates.
        var normalized = NormalizeWhitespace(commandText);
        // Build parameters in a stable order and format for reliable duplicate detection.
        var parameterSignature = BuildParameterSignature(command.Parameters);
        var callStack = CaptureCallStack();

        return new SqlQueryExecutionEntry(commandText, normalized, parameterSignature, rowCount, callStack);
    }

    private static string NormalizeCommandText(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText)) return string.Empty;

        var trimmed = commandText.Trim();
        return trimmed;
    }

    /// <summary>
    /// E.g. "SELECT  *" and "SELECT *" should count as the same query text.
    /// </summary>
    private static string NormalizeWhitespace(string text) =>
        WhitespaceRegex.Replace(text ?? string.Empty, " ").Trim();

    private static string CaptureCallStack()
    {
        var stackTrace = new StackTrace(fNeedFileInfo: true).ToString().TrimEnd();
        return string.IsNullOrWhiteSpace(stackTrace) ? null : stackTrace;
    }

    private static string BuildParameterSignature(DbParameterCollection parameters)
    {
        if (parameters == null || parameters.Count == 0) return "(no parameters)";

        // Parameter collection order can vary by provider, so sort by name for a stable signature. Use ordinal
        // ignore-case to avoid culture-sensitive differences.
        var items = parameters
            .Cast<DbParameter>()
            .OrderBy(parameter => parameter.ParameterName, StringComparer.OrdinalIgnoreCase)
            .Select(parameter => $"{parameter.ParameterName}={NormalizeParameterValue(parameter.Value)}");

        return string.Join(separator: "; ", values: items);
    }

    private static string NormalizeParameterValue(object value)
    {
        if (value == null) return "NULL";

        if (value is byte[] bytes) return $"byte[{bytes.Length}]";

        // Use culture-invariant values so the same value looks the same on every machine.
        if (value is DateTime dateTime) return dateTime.ToString("O", CultureInfo.InvariantCulture);

        if (value is DateTimeOffset dateTimeOffset) return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);

        if (value is IFormattable formattable)
        {
            return formattable.ToString(format: null, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WhitespaceRegex { get; }
}
