using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// A thread-safe, bounded store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    private const int MaxEntries = 50;

    private readonly Queue<SqlQueryMonitoringSummary> _summaries = new();
    private readonly object _lock = new();

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        lock (_lock)
        {
            _summaries.Enqueue(summary);
            TrimToCapacity();
        }
    }

    /// <summary>
    /// Removes and returns the newest stored summary.
    /// </summary>
    public bool TryDequeueMostRecent(out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(_ => true, out summary);

    /// <summary>
    /// Removes and returns the newest stored summary that has at least one SQL execution entry.
    /// </summary>
    public bool TryDequeueMostRecentWithExecutions(out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(candidate => candidate?.Executions.Count != 0, out summary);

    /// <summary>
    /// Removes and returns the newest stored summary matching the provided predicate.
    /// </summary>
    public bool TryDequeueMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(predicate, out summary);

    /// <summary>
    /// Finds the newest matching item, removes only that item from the queue, and preserves all remaining item
    /// ordering.
    /// </summary>
    private bool TryDequeueMostRecentCore(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_lock)
        {
            if (_summaries.Count == 0)
            {
                summary = null;
                return false;
            }

            var items = new List<SqlQueryMonitoringSummary>(_summaries);
            var index = items.FindLastIndex(predicate);

            if (index < 0)
            {
                summary = null;
                return false;
            }

            summary = items[index];
            items.RemoveAt(index);
            _summaries.Clear();

            foreach (var item in items)
            {
                _summaries.Enqueue(item);
            }

            return summary != null;
        }
    }

    private void TrimToCapacity()
    {
        while (_summaries.Count > MaxEntries)
        {
            if (!TryRemoveOldestSummary(candidate => candidate?.Executions.Count == 0))
            {
                _summaries.Dequeue();
            }
        }
    }

    private bool TryRemoveOldestSummary(Predicate<SqlQueryMonitoringSummary> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var items = new Queue<SqlQueryMonitoringSummary>(_summaries.Count);
        var removed = false;

        while (_summaries.Count > 0)
        {
            var candidate = _summaries.Dequeue();

            if (!removed && predicate(candidate))
            {
                removed = true;
                continue;
            }

            items.Enqueue(candidate);
        }

        while (items.Count > 0)
        {
            _summaries.Enqueue(items.Dequeue());
        }

        return removed;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _summaries.Clear();
        }
    }
}
