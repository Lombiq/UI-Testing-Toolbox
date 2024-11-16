using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public sealed class FakeLoggerLogApplicationLog : IApplicationLog
{
    public string Name => "FakeLog";
    public FakeLogCollector LogCollector { get; init; }
    public int EntryCount => LogCollector.Count;

    public Task<IEnumerable<IApplicationLogEntry>> GetEntriesAsync()
    {
        var records = LogCollector.GetSnapshot();

        return Task.FromResult(records.Select(record => (IApplicationLogEntry)new FakeLoggerApplicationLogEntry(record)));
    }

    public Task RemoveAsync()
    {
        LogCollector.Clear();
        return Task.CompletedTask;
    }
}

public record FakeLoggerApplicationLogEntry(FakeLogRecord LogRecord) : IApplicationLogEntry
{
    public LogLevel Level => LogRecord.Level;
    public EventId Id => LogRecord.Id;
    public Exception Exception => LogRecord.Exception;
    public string Message => LogRecord.Message;
    public string Category => LogRecord.Category;
    public DateTimeOffset Timestamp => LogRecord.Timestamp;

    public override string ToString() => FormatLogRecord(LogRecord);

    public static string FormatLogRecord(FakeLogRecord record) =>
        StringHelper.CreateInvariant($"{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{record.Level}] {record.Category}: {record.Message}") +
        (record.Exception != null ? record.Exception.ToString() : string.Empty);
}
