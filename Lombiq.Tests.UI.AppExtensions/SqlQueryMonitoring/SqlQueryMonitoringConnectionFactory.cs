using Microsoft.AspNetCore.Http;
using System;
using System.Data.Common;
using YesSql;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringConnectionFactory : IConnectionFactory
{
    private readonly IConnectionFactory _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringConnectionFactory(IConnectionFactory inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public DbConnection CreateConnection() =>
        new SqlQueryMonitoringDbConnection(_inner.CreateConnection(), _httpContextAccessor);

    public System.Type DbConnectionType => _inner.DbConnectionType;
}

internal static class SqlQueryMonitoringConnectionFactoryHelper
{
    public static void EnsureWrapped(IStore store, IHttpContextAccessor httpContextAccessor)
    {
        if (store?.Configuration?.ConnectionFactory == null) return;
        if (store.Configuration.ConnectionFactory is SqlQueryMonitoringConnectionFactory) return;

        store.Configuration.ConnectionFactory =
            new SqlQueryMonitoringConnectionFactory(store.Configuration.ConnectionFactory, httpContextAccessor);
    }
}
