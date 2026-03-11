using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Stores SQL query monitoring summaries for the current tenant scope.
/// </summary>
public interface ISqlQueryMonitoringStore
{
    /// <summary>
    /// Get readonly summaries.
    /// </summary>
    IReadOnlyList<SqlQueryMonitoringSummary> ReadonlySummaries();

    /// <summary>
    /// Adds a completed monitoring summary to the store.
    /// </summary>
    void AddSummary(SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Removes and returns the most recent summary matching the provided predicate, if any, while keeping other queued
    /// summaries.
    /// </summary>
    bool TryRemoveMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary);
}
