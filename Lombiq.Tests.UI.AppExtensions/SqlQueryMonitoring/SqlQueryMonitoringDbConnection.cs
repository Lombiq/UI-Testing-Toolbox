using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbConnection : DbConnection
{
    private readonly DbConnection _dbConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbConnection(DbConnection dbConnection, IHttpContextAccessor httpContextAccessor)
    {
        _dbConnection = dbConnection;
        _httpContextAccessor = httpContextAccessor;
    }

    public override string ConnectionString
    {
        get => _dbConnection.ConnectionString;
        set => _dbConnection.ConnectionString = value;
    }

    public override string Database => _dbConnection.Database;
    public override string DataSource => _dbConnection.DataSource;
    public override string ServerVersion => _dbConnection.ServerVersion;
    public override ConnectionState State => _dbConnection.State;

    public override void ChangeDatabase(string databaseName) => _dbConnection.ChangeDatabase(databaseName);

    public override void Close() => _dbConnection.Close();

    public override void Open() => _dbConnection.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => _dbConnection.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _dbConnection.BeginTransaction(isolationLevel);

    protected override DbCommand CreateDbCommand() =>
        new SqlQueryMonitoringDbCommand(_dbConnection.CreateCommand(), _httpContextAccessor);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dbConnection.Dispose();
        base.Dispose(disposing);
    }
}
