using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record CachedApplicationLog(string Name, IList<IApplicationLogEntry> Entries)
    : IApplicationLog
{
    public int EntryCount => Entries.Count;

    public Task<IEnumerable<IApplicationLogEntry>> GetEntriesAsync() => Task.FromResult(Entries.AsEnumerable());
    public Task RemoveAsync() => throw new NotSupportedException();

    public static Task<CachedApplicationLog> FromLogAsync(IApplicationLog log)
    {
        if (log.EntryCount < 1)
        {
            return Task.FromResult(new CachedApplicationLog(log.Name, []));
        }

        if (log is CachedApplicationLog cached)
        {
            return Task.FromResult(new CachedApplicationLog(log.Name, cached.Entries));
        }

        return FromLogInnerAsync(log);
    }

    private static async Task<CachedApplicationLog> FromLogInnerAsync(IApplicationLog log) =>
        new(log.Name, (await log.GetEntriesAsync()).AsList());
}
