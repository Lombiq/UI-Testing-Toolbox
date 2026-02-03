using Microsoft.AspNetCore.Http;
using System.Data.Common;
using YesSql;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringConnectionFactory : IConnectionFactory
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringConnectionFactory(IConnectionFactory inner, IHttpContextAccessor httpContextAccessor)
    {
        _connectionFactory = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbConnection CreateConnection() =>
        new SqlQueryMonitoringDbConnection(_connectionFactory.CreateConnection(), _httpContextAccessor);

    public System.Type DbConnectionType => _connectionFactory.DbConnectionType;
}
