using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// A thread-safe, bounded store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    // Keep the queue bounded so noisy request bursts don't grow memory usage without limit.
    // This is large enough for recent request matching, but small enough to avoid long stale history.
    private const int MaxEntries = 50;

    private readonly List<SqlQueryMonitoringSummary> _summaries = [];
    private readonly object _lock = new();

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        lock (_lock)
        {
            _summaries.Add(summary);
            TrimToCapacity();
        }
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
            if (_summaries.Count == 0)
            {
                summary = null;
                return false;
            }

            var index = _summaries.FindLastIndex(predicate);

            if (index < 0)
            {
                summary = null;
                return false;
            }

            summary = _summaries[index];
            _summaries.RemoveAt(index);

            return true;
        }
    }

    private void TrimToCapacity()
    {
        while (_summaries.Count > MaxEntries)
        {
            if (!TryRemoveOldestEmptySummary()) _summaries.RemoveAt(0);
        }
    }

    private bool TryRemoveOldestEmptySummary()
    {
        var index = _summaries.FindIndex(summary => summary.Executions.Count == 0);
        if (index < 0) return false;

        _summaries.RemoveAt(index);
        return true;
    }
}
