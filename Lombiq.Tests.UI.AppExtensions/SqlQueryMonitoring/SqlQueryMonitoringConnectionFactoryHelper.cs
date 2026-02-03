using Microsoft.AspNetCore.Http;
using YesSql;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

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
