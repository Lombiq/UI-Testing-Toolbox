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
        Func<IApplicationLogEntry, bool> predicate = null) =>
        FromLogAsync(log, predicate == null ? [] : [predicate]);

    public static Task<MemoryApplicationLog> FromLogAsync(
        IApplicationLog log,
        ICollection<Func<IApplicationLogEntry, bool>> predicates)
    {
        if (log.EntryCount < 1)
        {
            return Task.FromResult(new MemoryApplicationLog(log.Name, []));
        }

        if (log is MemoryApplicationLog cached)
        {
            return Task.FromResult(new MemoryApplicationLog(log.Name, FilterEntries(cached.Entries, predicates)));
        }

        return FromLogInnerAsync(log, predicates);
    }

    private static async Task<MemoryApplicationLog> FromLogInnerAsync(
        IApplicationLog log,
        ICollection<Func<IApplicationLogEntry, bool>> predicates) =>
        new(log.Name, FilterEntries(await log.GetEntriesAsync(), predicates));

    private static IList<IApplicationLogEntry> FilterEntries(
        IEnumerable<IApplicationLogEntry> entries,
        ICollection<Func<IApplicationLogEntry, bool>> predicates) =>
        predicates.Count == 0
            ? entries.AsList()
            : [.. entries.Where(entry => predicates.Aggregate(seed: true, (keep, predicate) => keep && predicate(entry)))];
}
