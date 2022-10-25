using Lombiq.Tests.UI.Services.Counters.Data;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.Counters.Extensions;

public static class ICounterDataCollectorExtensions
{
    public static int DbCommandExecuteNonQuery(this ICounterDataCollector collector, DbCommand dbCommand)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteNonQuery();
    }

    public static Task<int> DbCommandExecuteNonQueryAsync(
        this ICounterDataCollector collector,
        DbCommand dbCommand,
        CancellationToken cancellationToken)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public static object DbCommandExecuteScalar(this ICounterDataCollector collector, DbCommand dbCommand)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteScalar();
    }

    public static Task<object> DbCommandExecuteScalarAsync(
        this ICounterDataCollector collector,
        DbCommand dbCommand,
        CancellationToken cancellationToken)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteScalarAsync(cancellationToken);
    }

    public static DbDataReader DbCommandExecuteDbDataReader(
        this ICounterDataCollector collector,
        DbCommand dbCommand,
        CommandBehavior behavior)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteReader(behavior);
    }

    public static Task<DbDataReader> DbCommandExecuteDbDataReaderAsync(
        this ICounterDataCollector collector,
        DbCommand dbCommand,
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        collector.Increment(DbExecuteCounterKey.CreateFrom(dbCommand));
        return dbCommand.ExecuteReaderAsync(behavior, cancellationToken);
    }
}
