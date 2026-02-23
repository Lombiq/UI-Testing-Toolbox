using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbConnection : DbConnection
{
    public SqlQueryMonitoringDbConnection(DbConnection dbConnection, IHttpContextAccessor httpContextAccessor)
    {
        InnerConnection = dbConnection;
        HttpContextAccessor = httpContextAccessor;
    }

    internal DbConnection InnerConnection { get; }

    private IHttpContextAccessor HttpContextAccessor { get; }

    internal static DbConnection Unwrap(DbConnection dbConnection) =>
        dbConnection is SqlQueryMonitoringDbConnection monitoringDbConnection
            ? monitoringDbConnection.InnerConnection
            : dbConnection;

    public override string ConnectionString
    {
        get => InnerConnection.ConnectionString;
        set => InnerConnection.ConnectionString = value;
    }

    public override string Database => InnerConnection.Database;
    public override string DataSource => InnerConnection.DataSource;
    public override string ServerVersion => InnerConnection.ServerVersion;
    public override ConnectionState State => InnerConnection.State;

    public override void ChangeDatabase(string databaseName) => InnerConnection.ChangeDatabase(databaseName);

    public override void Close() => InnerConnection.Close();

    public override void Open() => InnerConnection.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => InnerConnection.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new SqlQueryMonitoringDbTransaction(InnerConnection.BeginTransaction(isolationLevel), this);

    protected override DbCommand CreateDbCommand() =>
        new SqlQueryMonitoringDbCommand(InnerConnection.CreateCommand(), HttpContextAccessor);

    protected override void Dispose(bool disposing)
    {
        if (disposing) InnerConnection.Dispose();
        base.Dispose(disposing);
    }
}
