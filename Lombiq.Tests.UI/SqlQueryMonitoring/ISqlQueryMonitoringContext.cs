using System.Collections.Generic;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Holds SQL query monitoring data for the current request scope.
/// </summary>
public interface ISqlQueryMonitoringContext
{
    /// <summary>
    /// Gets the SQL command executions captured for the current scope (typically a single HTTP request).
    /// </summary>
    IReadOnlyList<SqlQueryExecutionEntry> Executions { get; }

    /// <summary>
    /// Records a SQL command execution for later aggregation into the request summary.
    /// </summary>
    void RecordExecution(SqlQueryExecutionEntry entry);
}
