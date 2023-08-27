using Lombiq.Tests.UI.Services.Counters;
using Lombiq.Tests.UI.Services.Counters.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using YesSql;

namespace Lombiq.Tests.UI.Services;

public class ProbedConnectionFactory : IConnectionFactory
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ICounterDataCollector _counterDataCollector;

    public Type DbConnectionType => typeof(ProbedDbConnection);

    public ProbedConnectionFactory(IConnectionFactory connectionFactory, ICounterDataCollector counterDataCollector)
    {
        _connectionFactory = connectionFactory;
        _counterDataCollector = counterDataCollector;
    }

    public DbConnection CreateConnection()
    {
        var connection = _connectionFactory.CreateConnection();

        // This consition and the ProbedSqliteConnection can be removed once
        // https://github.com/OrchardCMS/OrchardCore/issues/14217 get fixed.
        return connection is SqliteConnection sqliteConnection
            ? new ProbedSqliteConnection(sqliteConnection, _counterDataCollector)
            : new ProbedDbConnection(connection, _counterDataCollector);
    }
}
