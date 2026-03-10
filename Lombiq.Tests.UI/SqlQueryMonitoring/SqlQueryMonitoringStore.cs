using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// A thread-safe store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    private readonly object _lock = new();
    private ConcurrentQueue<SqlQueryMonitoringSummary> _summaries = [];

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        _summaries.Enqueue(summary);
    }

    /// <summary>
    /// Removes and returns the newest stored summary matching the provided predicate.
    /// </summary>
    public bool TryRemoveMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary) =>
        TryRemoveMostRecentCore(predicate, out summary);

    /// <summary>
    /// Finds the newest matching item, removes only that item from the queue, and preserves all remaining item
    /// ordering.
    /// </summary>
    private bool TryRemoveMostRecentCore(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_lock)
        {
            var items = _summaries.ToList();

            if (items.Count == 0)
            {
                summary = null;
                return false;
            }

            var index = items.FindLastIndex(predicate);

            if (index < 0)
            {
                summary = null;
                return false;
            }

            summary = items[index];
            items.RemoveAt(index);
            _summaries = new ConcurrentQueue<SqlQueryMonitoringSummary>(items);
            return true;
        }
    }
}
