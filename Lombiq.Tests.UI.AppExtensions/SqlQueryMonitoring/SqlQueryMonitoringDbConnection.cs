using Microsoft.AspNetCore.Http;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbConnection : DbConnection
{
    private readonly DbConnection _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbConnection(DbConnection inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public override string ConnectionString
    {
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value;
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    public override void Close() => _inner.Close();

    public override void Open() => _inner.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _inner.BeginTransaction(isolationLevel);

    protected override DbCommand CreateDbCommand() =>
        new SqlQueryMonitoringDbCommand(_inner.CreateCommand(), _httpContextAccessor);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
