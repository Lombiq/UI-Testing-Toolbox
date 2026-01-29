using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringSummary
{
    public string RequestPath { get; }
    public string RequestMethod { get; }
    public string TraceIdentifier { get; }
    public DateTimeOffset CompletedUtc { get; }
    public IReadOnlyList<SqlQueryExecutionEntry> Executions { get; }

    public SqlQueryMonitoringSummary(
        string requestPath,
        string requestMethod,
        string traceIdentifier,
        DateTimeOffset completedUtc,
        IReadOnlyList<SqlQueryExecutionEntry> executions)
    {
        RequestPath = requestPath;
        RequestMethod = requestMethod;
        TraceIdentifier = traceIdentifier;
        CompletedUtc = completedUtc;
        Executions = executions;
    }
}
