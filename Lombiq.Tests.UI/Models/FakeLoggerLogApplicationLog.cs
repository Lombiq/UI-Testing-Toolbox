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
    public int MessageCount => LogCollector.Count;

    public Task<IEnumerable<IApplicationLogEntry>> GetEntriesAsync()
    {
        var records = LogCollector.GetSnapshot();

        return Task.FromResult(records.Select(record => (IApplicationLogEntry)new FakeLoggerApplicationLogEntry
        {
            Level = record.Level,
            Id = record.Id,
            Exception = record.Exception,
            Message = record.Message,
            Category = record.Category,
            Timestamp = record.Timestamp,
            LogRecord = record,
        }));
    }

    public Task RemoveAsync()
    {
        LogCollector.Clear();
        return Task.CompletedTask;
    }
}

public sealed class FakeLoggerApplicationLogEntry : IApplicationLogEntry
{
    public LogLevel Level { get; init; }
    public EventId Id { get; init; }
    public Exception Exception { get; init; }
    public string Message { get; init; }
    public string Category { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public FakeLogRecord LogRecord { get; init; }

    public override string ToString() =>
        $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Category}: {Message}" +
        (Exception != null ? Exception.ToString() : string.Empty);
}
