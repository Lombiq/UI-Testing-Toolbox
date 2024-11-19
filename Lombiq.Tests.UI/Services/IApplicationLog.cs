using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// An abstraction over a log, be it in the form of a file or something else.
/// </summary>
public interface IApplicationLog
{
    /// <summary>
    /// Gets the name of the log, such as the file name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the number of log entries in the log.
    /// </summary>
    int EntryCount { get; }

    /// <summary>
    /// Returns the content of the log, in case of log files reads the file contents.
    /// </summary>
    /// <returns>The contents.</returns>
    Task<IEnumerable<IApplicationLogEntry>> GetEntriesAsync();

    /// <summary>
    /// Removes the log if possible.
    /// </summary>
    Task RemoveAsync();
}

/// <summary>
/// An abstraction over a log entries.
/// </summary>
public interface IApplicationLogEntry
{
    /// <summary>
    /// Gets the level of the log entry, like <see cref="LogLevel.Error"/> or <see cref="LogLevel.Warning"/>.
    /// </summary>
    LogLevel Level { get; }

    /// <summary>
    /// Gets the ID that uniquely identifies the log entry.
    /// </summary>
    EventId Id { get; }

    /// <summary>
    /// Gets the exception associated with the log entry, if any.
    /// </summary>
    Exception Exception { get; }

    /// <summary>
    /// Gets the human-readable formatted log message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the category of the log entry. This is the type parameter of <see cref="ILogger{TCategoryName}"/>.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the timestamp of when the log entry was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}
