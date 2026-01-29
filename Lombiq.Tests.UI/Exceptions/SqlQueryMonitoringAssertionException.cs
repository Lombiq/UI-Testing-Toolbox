using Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Exceptions;

public class SqlQueryMonitoringAssertionException : Exception, IAssertionException
{
    public SqlQueryMonitoringSummary SqlQueryMonitoringSummary { get; }
    public SqlQueryMonitoringConfiguration SqlQueryMonitoringConfiguration { get; }

    public SqlQueryMonitoringAssertionException(
        SqlQueryMonitoringSummary summary,
        SqlQueryMonitoringConfiguration configuration,
        Exception innerException)
        : base(innerException?.Message, innerException)
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
