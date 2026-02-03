using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbDataReader : DbDataReader, IEnumerable<DbDataRecord>
{
    private readonly DbDataReader _dbDataReader;
    private readonly Action<int> _onCompleted;

    private int _rowCount;
    private bool _completed;

    public SqlQueryMonitoringDbDataReader(DbDataReader dbDataReader, Action<int> onCompleted)
    {
        _dbDataReader = dbDataReader;
        _onCompleted = onCompleted;
    }

    public override int Depth => _dbDataReader.Depth;
    public override int FieldCount => _dbDataReader.FieldCount;
    public override bool HasRows => _dbDataReader.HasRows;
    public override bool IsClosed => _dbDataReader.IsClosed;
    public override int RecordsAffected => _dbDataReader.RecordsAffected;

    public override object this[int ordinal] => _dbDataReader[ordinal];
    public override object this[string name] => _dbDataReader[name];

    public override bool Read()
    {
        var hasRow = _dbDataReader.Read();
        if (hasRow) _rowCount++;
        return hasRow;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var hasRow = await _dbDataReader.ReadAsync(cancellationToken);
        if (hasRow) _rowCount++;
        return hasRow;
    }

    public override bool NextResult() => _dbDataReader.NextResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
        _dbDataReader.NextResultAsync(cancellationToken);

    public override DataTable GetSchemaTable() => _dbDataReader.GetSchemaTable();
    public override string GetName(int ordinal) => _dbDataReader.GetName(ordinal);
    public override string GetDataTypeName(int ordinal) => _dbDataReader.GetDataTypeName(ordinal);
    public override Type GetFieldType(int ordinal) => _dbDataReader.GetFieldType(ordinal);
    public override object GetValue(int ordinal) => _dbDataReader.GetValue(ordinal);
    public override int GetValues(object[] values) => _dbDataReader.GetValues(values);
    public override int GetOrdinal(string name) => _dbDataReader.GetOrdinal(name);
    public override bool GetBoolean(int ordinal) => _dbDataReader.GetBoolean(ordinal);
    public override byte GetByte(int ordinal) => _dbDataReader.GetByte(ordinal);
    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
        _dbDataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    public override char GetChar(int ordinal) => _dbDataReader.GetChar(ordinal);
    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
        _dbDataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    public override Guid GetGuid(int ordinal) => _dbDataReader.GetGuid(ordinal);
    public override short GetInt16(int ordinal) => _dbDataReader.GetInt16(ordinal);
    public override int GetInt32(int ordinal) => _dbDataReader.GetInt32(ordinal);
    public override long GetInt64(int ordinal) => _dbDataReader.GetInt64(ordinal);
    public override float GetFloat(int ordinal) => _dbDataReader.GetFloat(ordinal);
    public override double GetDouble(int ordinal) => _dbDataReader.GetDouble(ordinal);
    public override string GetString(int ordinal) => _dbDataReader.GetString(ordinal);
    public override decimal GetDecimal(int ordinal) => _dbDataReader.GetDecimal(ordinal);
    public override DateTime GetDateTime(int ordinal) => _dbDataReader.GetDateTime(ordinal);
    public override bool IsDBNull(int ordinal) => _dbDataReader.IsDBNull(ordinal);

    public override IEnumerator GetEnumerator()
    {
        foreach (var record in _dbDataReader)
        {
            _rowCount++;
            yield return record;
        }
    }

    IEnumerator<DbDataRecord> IEnumerable<DbDataRecord>.GetEnumerator()
    {
        foreach (DbDataRecord record in _dbDataReader)
        {
            _rowCount++;
            yield return record;
        }
    }

    public override void Close()
    {
        _dbDataReader.Close();
        Complete();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dbDataReader.Dispose();
        Complete();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbDataReader.DisposeAsync();
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
