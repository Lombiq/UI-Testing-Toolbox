using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Represents a captured SQL monitoring summary for a single HTTP request.
/// </summary>
public sealed class SqlQueryMonitoringSummary
{
    /// <summary>
    /// Gets the tenant name that produced the summary.
    /// </summary>
    public string TenantName { get; }

    /// <summary>
    /// Gets the captured request path (optionally including query string).
    /// </summary>
    public string RequestPath { get; }

    /// <summary>
    /// Gets the captured HTTP request method.
    /// </summary>
    public string RequestMethod { get; }

    /// <summary>
    /// Gets the request trace identifier associated with this summary.
    /// </summary>
    public string TraceIdentifier { get; }

    /// <summary>
    /// Gets when summary capture finished.
    /// </summary>
    public DateTimeOffset CompletedUtc { get; }

    /// <summary>
    /// Gets the SQL execution entries captured for the request.
    /// </summary>
    public IReadOnlyList<SqlQueryExecutionEntry> Executions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlQueryMonitoringSummary"/> class.
    /// </summary>
    public SqlQueryMonitoringSummary(
        string tenantName,
        string requestPath,
        string requestMethod,
        string traceIdentifier,
        DateTimeOffset completedUtc,
        IReadOnlyList<SqlQueryExecutionEntry> executions)
    {
        TenantName = tenantName;
        RequestPath = requestPath;
        RequestMethod = requestMethod;
        TraceIdentifier = traceIdentifier;
        CompletedUtc = completedUtc;
        Executions = executions;
    }
}
