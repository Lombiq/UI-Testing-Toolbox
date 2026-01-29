using System.Collections.Concurrent;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public interface ISqlQueryMonitoringStore
{
    void AddSummary(SqlQueryMonitoringSummary summary);

    bool TryDequeueLatest(out SqlQueryMonitoringSummary summary);

    void Clear();
}

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
        }
    }

    private void Trim()
    {
        while (_summaries.Count > MaxEntries && _summaries.TryDequeue(out _))
        {
        }
    }
}
