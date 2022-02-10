using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if starts with the configured relative URL.
    /// </summary>
    [DebuggerDisplay("Starts with \"{_relativeUrlStartsWith}\"")]
    public class StartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string _relativeUrlStartsWith;

        public StartsWithMonkeyTestingUrlFilter(string relativeUrlStartsWith) =>
            _relativeUrlStartsWith = relativeUrlStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
            url.PathAndQuery.StartsWithOrdinalIgnoreCase(_relativeUrlStartsWith);
    }
}
