using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Wraps <see cref="DbConnection"/> so created commands and transactions are SQL-monitoring-aware.
/// </summary>
public sealed class SqlQueryMonitoringDbConnection : DbConnection
{
    private readonly DbConnection _innerDbConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbConnection(DbConnection dbConnection, IHttpContextAccessor httpContextAccessor)
    {
        _innerDbConnection = dbConnection;
        _httpContextAccessor = httpContextAccessor;
    }

    // If the connection is already wrapped by SQL monitoring, return the original inner connection.
    // This prevents double-wrapping and keeps provider operations working with the real connection type.
    internal static DbConnection Unwrap(DbConnection dbConnection) =>
        dbConnection is SqlQueryMonitoringDbConnection monitoringDbConnection
            ? monitoringDbConnection._innerDbConnection
            : dbConnection;

    public override string ConnectionString
    {
        get => _innerDbConnection.ConnectionString;
        set => _innerDbConnection.ConnectionString = value;
    }

    public override string Database => _innerDbConnection.Database;
    public override string DataSource => _innerDbConnection.DataSource;
    public override string ServerVersion => _innerDbConnection.ServerVersion;
    public override ConnectionState State => _innerDbConnection.State;

    public override void ChangeDatabase(string databaseName) => _innerDbConnection.ChangeDatabase(databaseName);

    public override void Close() => _innerDbConnection.Close();

    public override void Open() => _innerDbConnection.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => _innerDbConnection.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new SqlQueryMonitoringDbTransaction(_innerDbConnection.BeginTransaction(isolationLevel), this);

    protected override DbCommand CreateDbCommand() =>
        new SqlQueryMonitoringDbCommand(_innerDbConnection.CreateCommand(), _httpContextAccessor);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _innerDbConnection.Dispose();
        base.Dispose(disposing);
    }
}
