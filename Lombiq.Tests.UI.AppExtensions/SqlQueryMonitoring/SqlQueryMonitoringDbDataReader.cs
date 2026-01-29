using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbDataReader : DbDataReader
{
    private readonly DbDataReader _inner;
    private readonly Action<int> _onCompleted;
    private int _rowCount;
    private bool _completed;

    public SqlQueryMonitoringDbDataReader(DbDataReader inner, Action<int> onCompleted)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _onCompleted = onCompleted;
    }

    public override int Depth => _inner.Depth;
    public override int FieldCount => _inner.FieldCount;
    public override bool HasRows => _inner.HasRows;
    public override bool IsClosed => _inner.IsClosed;
    public override int RecordsAffected => _inner.RecordsAffected;

    public override object this[int ordinal] => _inner[ordinal];
    public override object this[string name] => _inner[name];

    public override bool Read()
    {
        var hasRow = _inner.Read();
        if (hasRow) _rowCount++;
        return hasRow;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var hasRow = await _inner.ReadAsync(cancellationToken);
        if (hasRow) _rowCount++;
        return hasRow;
    }

    public override bool NextResult() => _inner.NextResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
        _inner.NextResultAsync(cancellationToken);

    public override DataTable GetSchemaTable() => _inner.GetSchemaTable();
    public override string GetName(int ordinal) => _inner.GetName(ordinal);
    public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);
    public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
    public override object GetValue(int ordinal) => _inner.GetValue(ordinal);
    public override int GetValues(object[] values) => _inner.GetValues(values);
    public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
    public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
    public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
        _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
        _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
    public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
    public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);
    public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
    public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
    public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
    public override string GetString(int ordinal) => _inner.GetString(ordinal);
    public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
    public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
    public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);

    public override IEnumerator GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

    public override void Close()
    {
        _inner.Close();
        Complete();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        Complete();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        Complete();
        await base.DisposeAsync();
    }

    private void Complete()
    {
        if (_completed) return;
        _completed = true;
        _onCompleted?.Invoke(_rowCount);
    }
}
