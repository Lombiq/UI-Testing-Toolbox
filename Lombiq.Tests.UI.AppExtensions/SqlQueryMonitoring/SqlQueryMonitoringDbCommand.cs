using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbCommand : DbCommand
{
    private readonly DbCommand _dbCommand;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlQueryMonitoringDbCommand(DbCommand dbCommand, IHttpContextAccessor httpContextAccessor)
    {
        _dbCommand = dbCommand;
        _httpContextAccessor = httpContextAccessor;
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Command text is set by the underlying data access layer, not user input.")]
    public override string CommandText
    {
        get => _dbCommand.CommandText;
        set => _dbCommand.CommandText = value;
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
        set => _dbCommand.Connection = value;
    }

    protected override DbTransaction DbTransaction
    {
        get => _dbCommand.Transaction;
        set => _dbCommand.Transaction = value;
    }

    protected override DbParameterCollection DbParameterCollection => _dbCommand.Parameters;

    public override void Cancel() => _dbCommand.Cancel();

    public override int ExecuteNonQuery()
    {
        var result = _dbCommand.ExecuteNonQuery();
        RecordExecution(rowCount: null);
        return result;
    }

    public override object ExecuteScalar()
    {
        var result = _dbCommand.ExecuteScalar();
        RecordExecution(rowCount: null);
        return result;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        var task = _dbCommand.ExecuteNonQueryAsync(cancellationToken);
        return RecordAfterAsync(task, rowCount: null);
    }

    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var task = _dbCommand.ExecuteScalarAsync(cancellationToken);
        return RecordAfterAsync(task, rowCount: null);
    }

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

        monitor.RecordExecution(SqlQueryExecutionEntry.FromCommand(_dbCommand, rowCount));
    }
}
