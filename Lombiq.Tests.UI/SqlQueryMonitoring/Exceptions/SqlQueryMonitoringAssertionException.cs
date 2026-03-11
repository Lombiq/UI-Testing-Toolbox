using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Services;
using System;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;

public class SqlQueryMonitoringAssertionException : Exception, IAssertionException
{
    public SqlQueryMonitoringSummary SqlQueryMonitoringSummary { get; }
    public SqlQueryMonitoringConfiguration SqlQueryMonitoringConfiguration { get; }

    public SqlQueryMonitoringAssertionException(
        SqlQueryMonitoringSummary summary,
        SqlQueryMonitoringConfiguration configuration,
        string message)
        : base(message)
    {
        SqlQueryMonitoringSummary = summary;
        SqlQueryMonitoringConfiguration = configuration;
    }

    public SqlQueryMonitoringAssertionException(string message)
        : base(message)
    {
    }

    public SqlQueryMonitoringAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SqlQueryMonitoringAssertionException()
    {
    }
}
