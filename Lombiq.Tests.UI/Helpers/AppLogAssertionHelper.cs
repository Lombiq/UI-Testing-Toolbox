using Lombiq.Tests.UI.Services;
using System;
using System.Linq.Expressions;

namespace Lombiq.Tests.UI.Helpers;

public static class AppLogAssertionHelper
{
    /// <summary>
    /// An <see cref="Expression"/> wrapping <see cref="NotMediaCacheEntries(IApplicationLogEntry)"/>.
    /// </summary>
    [Obsolete("This is no longer necessary after https://github.com/OrchardCMS/OrchardCore/pull/18341.")]
    public static readonly Expression<Func<IApplicationLogEntry, bool>> NotMediaCacheEntriesPredicate =
        logEntry => NotMediaCacheEntries(logEntry);

    /// <summary>
    /// Creates a predicate that can be used to filter out "Error deleting cache folder" log entries coming from
    /// <c>DefaultMediaFileStoreCacheFileProvider</c>. These errors frequently happen during UI testing when using Azure
    /// Blob Storage for media storage. They're harmless, though.
    /// </summary>
    [Obsolete("This is no longer necessary after https://github.com/OrchardCMS/OrchardCore/pull/18341.")]
    public static bool NotMediaCacheEntries(IApplicationLogEntry logEntry) =>
        logEntry.Category != "OrchardCore.Media.Core.DefaultMediaFileStoreCacheFileProvider" ||
        !logEntry.Message.StartsWithOrdinalIgnoreCase("Error deleting cache folder");
}
