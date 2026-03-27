using System;
using System.Collections.Generic;

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
    /// Finds and returns the newest most recent matching item.
    /// </summary>
    bool TryGetMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Returns all summaries matching the provided predicate, if any, while keeping all summaries in the store.
    /// </summary>
    bool TryGetMostRecentMatches(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out IList<SqlQueryMonitoringSummary> summary);
}
