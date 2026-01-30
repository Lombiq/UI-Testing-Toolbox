using System.Collections.Generic;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringContext : ISqlQueryMonitoringContext
{
    private readonly List<SqlQueryExecutionEntry> _executions = [];
    private readonly object _lock = new();

    public IReadOnlyList<SqlQueryExecutionEntry> Executions
    {
        get
        {
            lock (_lock)
            {
                return [.. _executions];
            }
        }
    }

    public void RecordExecution(SqlQueryExecutionEntry entry)
    {
        if (entry == null) return;

        lock (_lock)
        {
            _executions.Add(entry);
        }
    }
}
