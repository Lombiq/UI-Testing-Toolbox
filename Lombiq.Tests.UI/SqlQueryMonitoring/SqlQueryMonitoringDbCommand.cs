using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Wraps <see cref="DbCommand"/> so SQL monitoring can record executed SQL and row counts.
/// </summary>
public sealed class SqlQueryMonitoringDbCommand : DbCommand
{
    private readonly DbCommand _dbCommand;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbCommand(DbCommand dbCommand, IHttpContextAccessor httpContextAccessor)
    {
        _dbCommand = dbCommand;
        _httpContextAccessor = httpContextAccessor;
    }

    public override string CommandText
    {
        get => _dbCommand.CommandText;

        // Command text is set by the underlying data access layer, not user input.
#pragma warning disable CA2100 // CA2100: Review if the query string passed to 'string DbCommand.CommandText' in 'set_CommandText'
        set => _dbCommand.CommandText = value;
#pragma warning restore CA2100
    }

    public override int CommandTimeout
    {
        get => _dbCommand.CommandTimeout;
        set => _dbCommand.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _dbCommand.CommandType;
        set => _dbCommand.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _dbCommand.DesignTimeVisible;
        set => _dbCommand.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _dbCommand.UpdatedRowSource;
        set => _dbCommand.UpdatedRowSource = value;
    }

    protected override DbConnection DbConnection
    {
        get => _dbCommand.Connection;
        set => _dbCommand.Connection = SqlQueryMonitoringDbConnection.Unwrap(value);
    }

    protected override DbTransaction DbTransaction
    {
        get => _dbCommand.Transaction;
        set => _dbCommand.Transaction = SqlQueryMonitoringDbTransaction.Unwrap(value);
    }

    protected override DbParameterCollection DbParameterCollection => _dbCommand.Parameters;

    public override void Cancel() => _dbCommand.Cancel();

    public override int ExecuteNonQuery() =>
        ExecuteAndRecord(_dbCommand.ExecuteNonQuery);

    public override object ExecuteScalar() =>
        ExecuteAndRecord(_dbCommand.ExecuteScalar);

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        ExecuteAndRecordAsync(() => _dbCommand.ExecuteNonQueryAsync(cancellationToken));

    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        ExecuteAndRecordAsync(() => _dbCommand.ExecuteScalarAsync(cancellationToken));

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var reader = _dbCommand.ExecuteReader(behavior);
        return new SqlQueryMonitoringDbDataReader(reader, rowCount => RecordExecution(rowCount));
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        var reader = await _dbCommand.ExecuteReaderAsync(behavior, cancellationToken);
        return new SqlQueryMonitoringDbDataReader(reader, rowCount => RecordExecution(rowCount));
    }

    public override void Prepare() => _dbCommand.Prepare();

    protected override DbParameter CreateDbParameter() => _dbCommand.CreateParameter();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dbCommand.Dispose();
        base.Dispose(disposing);
    }

    private void RecordExecution(int? rowCount)
    {
        var monitor = _httpContextAccessor.HttpContext?.RequestServices.GetService<ISqlQueryMonitoringContext>();
        if (monitor == null) return;

        monitor.RecordExecution(SqlQueryExecutionEntry.FromCommand(_dbCommand, rowCount));
    }

    private T ExecuteAndRecord<T>(Func<T> execute)
    {
        var result = execute();
        RecordExecution(rowCount: null);
        return result;
    }

    private async Task<T> ExecuteAndRecordAsync<T>(Func<Task<T>> executeAsync)
    {
        var result = await executeAsync();
        RecordExecution(rowCount: null);
        return result;
    }
}
