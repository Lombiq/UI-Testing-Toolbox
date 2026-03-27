using Microsoft.AspNetCore.Http;
using YesSql;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;

internal static class SqlQueryMonitoringConnectionFactoryHelper
{
    public static void EnsureWrapped(IStore store, IHttpContextAccessor httpContextAccessor)
    {
        var connectionFactory = store?.Configuration?.ConnectionFactory;
        if (connectionFactory is null or SqlQueryMonitoringConnectionFactory) return;

        store.Configuration.ConnectionFactory =
            new SqlQueryMonitoringConnectionFactory(connectionFactory, httpContextAccessor);
    }
}
