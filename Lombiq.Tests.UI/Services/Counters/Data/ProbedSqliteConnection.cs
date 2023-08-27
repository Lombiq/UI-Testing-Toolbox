using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;
using System.Data.Common;

namespace Lombiq.Tests.UI.Services.Counters.Data;

// This is required to avoid the component being visible in VS Toolbox.
[DesignerCategory("")]
public class ProbedSqliteConnection : SqliteConnection
{
    private readonly ICounterDataCollector _counterDataCollector;

    internal SqliteConnection ProbedConnection { get; private set; }

    public ProbedSqliteConnection(SqliteConnection connection, ICounterDataCollector counterDataCollector)
        : base(connection.ConnectionString) =>
        _counterDataCollector = counterDataCollector ?? throw new ArgumentNullException(nameof(counterDataCollector));

    protected virtual DbCommand CreateDbCommand(DbCommand original) =>
        new ProbedDbCommand(original, this, _counterDataCollector);

    protected override DbCommand CreateDbCommand() =>
        CreateDbCommand(CreateCommand());

    protected override void Dispose(bool disposing)
    {
        if (disposing && ProbedConnection is not null)
        {
            ProbedConnection.Dispose();
        }

        base.Dispose(disposing);
        ProbedConnection = null;
    }
}
