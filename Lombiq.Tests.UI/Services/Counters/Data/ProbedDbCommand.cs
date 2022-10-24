using Lombiq.Tests.UI.Extensions;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.Counters.Data;

[DesignerCategory("")]
public class ProbedDbCommand : DbCommand
{
    private readonly ICounterDataCollector _counterDataCollector;
    private DbConnection _dbConnection;

    internal DbCommand ProbedCommand { get; private set; }

    // The ProbedDbCommand scope is performance counting.
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
    public override string CommandText
    {
        get => ProbedCommand.CommandText;
        set => ProbedCommand.CommandText = value;
    }
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

    public override int CommandTimeout
    {
        get => ProbedCommand.CommandTimeout;
        set => ProbedCommand.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => ProbedCommand.CommandType;
        set => ProbedCommand.CommandType = value;
    }

    protected override DbConnection DbConnection
    {
        get => _dbConnection;
        set
        {
            _dbConnection = value;
            UnwrapAndAssignConnection(value);
        }
    }

    protected override DbParameterCollection DbParameterCollection => ProbedCommand.Parameters;

    protected override DbTransaction DbTransaction
    {
        get => ProbedCommand.Transaction;
        set => ProbedCommand.Transaction = value;
    }

    public override bool DesignTimeVisible
    {
        get => ProbedCommand.DesignTimeVisible;
        set => ProbedCommand.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => ProbedCommand.UpdatedRowSource;
        set => ProbedCommand.UpdatedRowSource = value;
    }

    public ProbedDbCommand(DbCommand command, DbConnection connection, ICounterDataCollector counterDataCollector)
    {
        _counterDataCollector = counterDataCollector ?? throw new ArgumentNullException(nameof(counterDataCollector));
        ProbedCommand = command ?? throw new ArgumentNullException(nameof(command));

        if (connection != null)
        {
            _dbConnection = connection;
            UnwrapAndAssignConnection(connection);
        }
    }

    public override int ExecuteNonQuery() =>
        _counterDataCollector.DbCommandExecuteNonQuery(ProbedCommand);

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        _counterDataCollector.DbCommandExecuteNonQueryAsync(ProbedCommand, cancellationToken);

    public override object ExecuteScalar() =>
        _counterDataCollector.DbCommandExecuteScalar(ProbedCommand);

    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        _counterDataCollector.DbCommandExecuteScalarAsync(ProbedCommand, cancellationToken);

    public override void Cancel() => ProbedCommand.Cancel();

    public override void Prepare() => ProbedCommand.Prepare();

    protected override DbParameter CreateDbParameter() => ProbedCommand.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        new ProbedDbDataReader(
            _counterDataCollector.DbCommandExecuteDbDatareader(ProbedCommand, behavior),
            behavior,
            this,
            _counterDataCollector);

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken) =>
        new ProbedDbDataReader(
            await _counterDataCollector.DbCommandExecuteDbDatareaderAsync(ProbedCommand, behavior, cancellationToken),
            behavior,
            this,
            _counterDataCollector);

    private void UnwrapAndAssignConnection(DbConnection value) =>
        ProbedCommand.Connection = value is ProbedDbConnection probedConnection
            ? probedConnection.ProbedConnection
            : value;

    protected override void Dispose(bool disposing)
    {
        if (disposing && ProbedCommand != null)
        {
            ProbedCommand.Dispose();
        }

        ProbedCommand = null;
        base.Dispose(disposing);
    }
}
