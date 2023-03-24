using Lombiq.Tests.UI.Services.Counters;
using Lombiq.Tests.UI.Services.Counters.Data;
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

    public DbConnection CreateConnection() =>
        new ProbedDbConnection(_connectionFactory.CreateConnection(), _counterDataCollector);
}