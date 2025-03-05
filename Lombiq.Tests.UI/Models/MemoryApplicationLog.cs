using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// An <see cref="IApplicationLog"/> implementation where the entries are stored in memory. This makes the logs easier to inspect in IDE during
/// debugging and working with a list of this type is easier to filter and aggregate than the base type.
/// </summary>
public record MemoryApplicationLog(string Name, IList<IApplicationLogEntry> Entries)
    : IApplicationLog
{
    public int EntryCount => Entries.Count;

    public Task<IEnumerable<IApplicationLogEntry>> GetEntriesAsync() => Task.FromResult(Entries.AsEnumerable());
    public Task RemoveAsync() => throw new NotSupportedException();

    /// <summary>
    /// Converts an existing <paramref name="log"/> into in-memory log by eagerly fetching its contents. Use this to
    /// avoid having to <see langword="await"/> <see cref="IApplicationLog.GetEntriesAsync"/> multiple times.
    /// </summary>
    public static Task<MemoryApplicationLog> FromLogAsync(
        IApplicationLog log,
        Func<IApplicationLogEntry, bool> predicate = null)
    {
        if (log.EntryCount < 1)
        {
            return Task.FromResult(new MemoryApplicationLog(log.Name, []));
        }

        if (log is MemoryApplicationLog cached)
        {
            return Task.FromResult(new MemoryApplicationLog(log.Name, FilterEntries(cached.Entries, predicate)));
        }

        return FromLogInnerAsync(log, predicate);
    }

    private static async Task<MemoryApplicationLog> FromLogInnerAsync(
        IApplicationLog log,
        Func<IApplicationLogEntry, bool> predicate) =>
        new(log.Name, FilterEntries(await log.GetEntriesAsync(), predicate));

    private static IList<IApplicationLogEntry> FilterEntries(
        IEnumerable<IApplicationLogEntry> entries,
        Func<IApplicationLogEntry, bool> predicate) =>
        predicate is null ? entries.AsList() : entries.Where(predicate).ToList();
}
