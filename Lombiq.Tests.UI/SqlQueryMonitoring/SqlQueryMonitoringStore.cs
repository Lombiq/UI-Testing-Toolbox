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
            while (_summaries.Count > MaxEntries) _summaries.Dequeue();
        }
    }

    public bool TryDequeueMostRecent(out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(_ => true, out summary);

    public bool TryDequeueMostRecentWithExecutions(out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(candidate => candidate?.Executions.Count != 0, out summary);

    public bool TryDequeueMostRecentMatching(
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary) =>
        TryDequeueMostRecentCore(predicate, out summary);

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

    public void Clear()
    {
        lock (_lock)
        {
            _summaries.Clear();
        }
    }
}
