using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbCommand : DbCommand
{
    private readonly DbCommand _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbCommand(DbCommand inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Command text is set by the underlying data access layer, not user input.")]
    public override string CommandText
    {
        get => _inner.CommandText;
        set => _inner.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _inner.DesignTimeVisible;
        set => _inner.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    protected override DbConnection DbConnection
    {
        get => _inner.Connection;
        set => _inner.Connection = value;
    }

    protected override DbTransaction DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value;
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    public override void Cancel() => _inner.Cancel();

    public override int ExecuteNonQuery()
    {
        var result = _inner.ExecuteNonQuery();
        RecordExecution(rowCount: null);
        return result;
    }

    public override object ExecuteScalar()
    {
        var result = _inner.ExecuteScalar();
        RecordExecution(rowCount: null);
        return result;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        var task = _inner.ExecuteNonQueryAsync(cancellationToken);
        return RecordAfterAsync(task, rowCount: null);
    }

    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var task = _inner.ExecuteScalarAsync(cancellationToken);
        return RecordAfterAsync(task, rowCount: null);
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var reader = _inner.ExecuteReader(behavior);
        return new SqlQueryMonitoringDbDataReader(reader, rowCount => RecordExecution(rowCount));
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        var reader = await _inner.ExecuteReaderAsync(behavior, cancellationToken);
        return new SqlQueryMonitoringDbDataReader(reader, rowCount => RecordExecution(rowCount));
    }

    public override void Prepare() => _inner.Prepare();

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }

    [SuppressMessage(
        "Usage",
        "VSTHRD003:Avoid awaiting foreign Tasks",
        Justification = "Awaiting the underlying database operation is required to record the execution.")]
    private async Task<int> RecordAfterAsync(Task<int> task, int? rowCount)
    {
        var result = await task;
        RecordExecution(rowCount);
        return result;
    }

    [SuppressMessage(
        "Usage",
        "VSTHRD003:Avoid awaiting foreign Tasks",
        Justification = "Awaiting the underlying database operation is required to record the execution.")]
    private async Task<object> RecordAfterAsync(Task<object> task, int? rowCount)
    {
        var result = await task;
        RecordExecution(rowCount);
        return result;
    }

    private void RecordExecution(int? rowCount)
    {
        var monitor = _httpContextAccessor.HttpContext?.RequestServices.GetService<ISqlQueryMonitoringContext>();
        if (monitor == null) return;

        monitor.RecordExecution(SqlQueryExecutionEntry.FromCommand(_inner, rowCount));
    }
}
