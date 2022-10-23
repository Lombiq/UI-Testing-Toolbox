using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.Counters.Data;

// Generic IEnumerable<> interface is not implemented in DbDatareader.
#pragma warning disable CA1010 // Generic interface should also be implemented
public class ProbedDbDataReader : DbDataReader
#pragma warning restore CA1010 // Generic interface should also be implemented
{
    private CounterDataCollector CounterDataCollector { get; init; }

    private ProbedDbCommand ProbedCommand { get; init; }

    private DbReadCounterKey CounterKey { get; init; }

    internal DbDataReader ProbedReader { get; private set; }

    public CommandBehavior Behavior { get; }

    public override int Depth => ProbedReader.Depth;

    public override int FieldCount => ProbedReader.FieldCount;

    public override bool HasRows => ProbedReader.HasRows;

    public override bool IsClosed => ProbedReader.IsClosed;

    public override int RecordsAffected => ProbedReader.RecordsAffected;

    public override object this[string name] => ProbedReader[name];

    public override object this[int ordinal] => ProbedReader[ordinal];

    public ProbedDbDataReader(
        DbDataReader reader,
        ProbedDbCommand probedCommand,
        CounterDataCollector counterDataCollector)
        : this(reader, CommandBehavior.Default, probedCommand, counterDataCollector) { }

    public ProbedDbDataReader(
        DbDataReader reader,
        CommandBehavior behavior,
        ProbedDbCommand probedCommand,
        CounterDataCollector counterDataCollector)
    {
        CounterDataCollector = counterDataCollector ?? throw new ArgumentNullException(nameof(counterDataCollector));
        ProbedCommand = probedCommand ?? throw new ArgumentNullException(nameof(probedCommand));
        ProbedReader = reader ?? throw new ArgumentNullException(nameof(reader));
        Behavior = behavior;
        CounterKey = DbReadCounterKey.CreateFrom(ProbedCommand);
    }

    public override bool GetBoolean(int ordinal) => ProbedReader.GetBoolean(ordinal);

    public override byte GetByte(int ordinal) => ProbedReader.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
        ProbedReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    public override char GetChar(int ordinal) => ProbedReader.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
        ProbedReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    public new DbDataReader GetData(int ordinal) => ProbedReader.GetData(ordinal);

    public override string GetDataTypeName(int ordinal) => ProbedReader.GetDataTypeName(ordinal);

    public override DateTime GetDateTime(int ordinal) => ProbedReader.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal) => ProbedReader.GetDecimal(ordinal);

    public override double GetDouble(int ordinal) => ProbedReader.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => new ReaderEnumerator(this);

    public override Type GetFieldType(int ordinal) => ProbedReader.GetFieldType(ordinal);

    public override T GetFieldValue<T>(int ordinal) => ProbedReader.GetFieldValue<T>(ordinal);

    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) =>
        ProbedReader.GetFieldValueAsync<T>(ordinal, cancellationToken);

    public override float GetFloat(int ordinal) => ProbedReader.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal) => ProbedReader.GetGuid(ordinal);

    public override short GetInt16(int ordinal) => ProbedReader.GetInt16(ordinal);

    public override int GetInt32(int ordinal) => ProbedReader.GetInt32(ordinal);

    public override long GetInt64(int ordinal) => ProbedReader.GetInt64(ordinal);

    public override string GetName(int ordinal) => ProbedReader.GetName(ordinal);

    public override int GetOrdinal(string name) => ProbedReader.GetOrdinal(name);

    public override string GetString(int ordinal) => ProbedReader.GetString(ordinal);

    public override object GetValue(int ordinal) => ProbedReader.GetValue(ordinal);

    public override int GetValues(object[] values) => ProbedReader.GetValues(values);

    public override bool IsDBNull(int ordinal) => ProbedReader.IsDBNull(ordinal);

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) =>
        ProbedReader.IsDBNullAsync(ordinal, cancellationToken);

    public override bool NextResult() => ProbedReader.NextResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
        ProbedReader.NextResultAsync(cancellationToken);

    public override bool Read()
    {
        var result = ProbedReader.Read();
        if (result) CounterDataCollector.Increment(CounterKey);

        return result;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var result = await ProbedReader.ReadAsync(cancellationToken);
        if (result) CounterDataCollector.Increment(CounterKey);

        return result;
    }

    public override void Close() => ProbedReader.Close();

    public override DataTable GetSchemaTable() => ProbedReader.GetSchemaTable();

    protected override void Dispose(bool disposing)
    {
        ProbedReader.Dispose();
        base.Dispose(disposing);
    }

    private sealed class ReaderEnumerator : IEnumerator
    {
        private readonly ProbedDbDataReader _probedReader;
        private readonly IEnumerator _probedEnumerator;

        public ReaderEnumerator(ProbedDbDataReader probedReader)
        {
            _probedReader = probedReader;
            _probedEnumerator = (probedReader.ProbedReader as IEnumerable).GetEnumerator();
        }

        public object Current => _probedEnumerator.Current;

        public bool MoveNext()
        {
            var haveNext = _probedEnumerator.MoveNext();
            if (haveNext) _probedReader.CounterDataCollector.Increment(_probedReader.CounterKey);

            return haveNext;
        }

        public void Reset() => _probedEnumerator.Reset();
    }
}
