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
    /// Removes and returns the most recent summary from the store, if any.
    /// </summary>
    bool TryDequeueLatest(out SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Clears all stored summaries.
    /// </summary>
    void Clear();
}
