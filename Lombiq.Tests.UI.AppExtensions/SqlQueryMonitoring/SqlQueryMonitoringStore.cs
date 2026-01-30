using System.Collections.Concurrent;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

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

/// <summary>
/// A thread-safe FIFO store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    private const int MaxEntries = 50;

    private readonly ConcurrentQueue<SqlQueryMonitoringSummary> _summaries = new();

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        _summaries.Enqueue(summary);
        Trim();
    }

    public bool TryDequeueLatest(out SqlQueryMonitoringSummary summary)
    {
        summary = null;
        SqlQueryMonitoringSummary summaryWithExecutions = null;

        while (_summaries.TryDequeue(out var current))
        {
            summary = current;
            if (current.Executions.Count != 0) summaryWithExecutions = current;
        }

        summary = summaryWithExecutions ?? summary;
        return summary != null;
    }

    public void Clear()
    {
        while (_summaries.TryDequeue(out _))
        {
            // Drain the queue.
        }
    }

    private void Trim()
    {
        while (_summaries.Count > MaxEntries && _summaries.TryDequeue(out _))
        {
            // Keep only the most recent entries.
        }
    }
}
