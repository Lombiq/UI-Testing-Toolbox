using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// A thread-safe store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    private readonly ConcurrentBag<SqlQueryMonitoringSummary> _summaries = [];

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        _summaries.Add(summary);
    }

    public bool TryGetMostRecentMatching(Predicate<SqlQueryMonitoringSummary> predicate, out SqlQueryMonitoringSummary summary)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        summary = _summaries.Where(summary => predicate(summary)).OrderByDescending(summary => summary.CompletedUtc).FirstOrDefault();

        return summary != null;
    }

    public bool TryGetMostRecentMatches(Predicate<SqlQueryMonitoringSummary> predicate, out IList<SqlQueryMonitoringSummary> summary)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        summary = [.. _summaries.Where(summary => predicate(summary)).OrderByDescending(summary => summary.CompletedUtc)];

        return summary != null;
    }
}
