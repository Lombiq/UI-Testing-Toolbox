using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Lombiq.Tests.UI.Services.Counters.Data;

[DesignerCategory("")]
public class ProbedDbConnection : DbConnection
{
    private readonly CounterDataCollector _counterDataCollector;

    internal DbConnection ProbedConnection { get; private set; }

    public override string ConnectionString
    {
        get => ProbedConnection.ConnectionString;
        set => ProbedConnection.ConnectionString = value;
    }

    public override int ConnectionTimeout => ProbedConnection.ConnectionTimeout;

    public override string Database => ProbedConnection.Database;

    public override string DataSource => ProbedConnection.DataSource;

    public override string ServerVersion => ProbedConnection.ServerVersion;

    public override ConnectionState State => ProbedConnection.State;

    protected override bool CanRaiseEvents => true;

    public ProbedDbConnection(DbConnection connection, CounterDataCollector counterDataCollector)
    {
        _counterDataCollector = counterDataCollector ?? throw new ArgumentNullException(nameof(counterDataCollector));
        ProbedConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        ProbedConnection.StateChange += StateChangeHandler;
    }

    public override void ChangeDatabase(string databaseName) =>
        ProbedConnection.ChangeDatabase(databaseName);

    public override void Close() =>
        ProbedConnection.Close();

    public override void Open() =>
        ProbedConnection.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) =>
        ProbedConnection.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) =>
        ProbedConnection.BeginTransaction(isolationLevel);

    protected virtual DbCommand CreateDbCommand(DbCommand original) =>
        new ProbedDbCommand(original, this, _counterDataCollector);

    protected override DbCommand CreateDbCommand() =>
        CreateDbCommand(ProbedConnection.CreateCommand());

    private void StateChangeHandler(object sender, StateChangeEventArgs stateChangeEventArguments) =>
        OnStateChange(stateChangeEventArguments);

    public override void EnlistTransaction(Transaction transaction) =>
        ProbedConnection.EnlistTransaction(transaction);

    public override DataTable GetSchema() =>
        ProbedConnection.GetSchema();

    public override DataTable GetSchema(string collectionName) =>
        ProbedConnection.GetSchema(collectionName);

    public override DataTable GetSchema(string collectionName, string[] restrictionValues) =>
        ProbedConnection.GetSchema(collectionName, restrictionValues);

    protected override void Dispose(bool disposing)
    {
        if (disposing && ProbedConnection != null)
        {
            ProbedConnection.StateChange -= StateChangeHandler;
            ProbedConnection.Dispose();
        }

        base.Dispose(disposing);
        ProbedConnection = null;
    }
}
