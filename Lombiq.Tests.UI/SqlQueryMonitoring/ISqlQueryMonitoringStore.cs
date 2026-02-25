using System;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Stores SQL query monitoring summaries for the current tenant scope.
/// </summary>
public interface ISqlQueryMonitoringStore
{
    /// <summary>
    /// Adds a completed monitoring summary to the store.
    /// </summary>
    void AddSummary(SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Removes and returns the most recent summary that contains SQL executions, if any.
    /// </summary>
    bool TryDequeueMostRecentWithExecutions(out SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Removes and returns the most recent summary matching the provided predicate, if any, while keeping other queued
    /// summaries.
    /// </summary>
    bool TryDequeueMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Clears all stored summaries.
    /// </summary>
    void Clear();
}
