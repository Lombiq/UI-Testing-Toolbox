using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringContext : ISqlQueryMonitoringContext
{
    private readonly ConcurrentQueue<SqlQueryExecutionEntry> _executions = new();

    public IReadOnlyList<SqlQueryExecutionEntry> Executions
        => _executions.ToArray();

    public void RecordExecution(SqlQueryExecutionEntry entry)
    {
        if (entry == null) return;
        _executions.Enqueue(entry);
    }
}
