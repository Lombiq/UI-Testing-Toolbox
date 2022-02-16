using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics;
using System.Linq;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if starts with the configured relative URL(s).
    /// </summary>
    [DebuggerDisplay("Starts with {_relativeUrlStartsWith}")]
    public class StartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string[] _relativeUrlStartsWith;

        public StartsWithMonkeyTestingUrlFilter(params string[] relativeUrlStartsWith) =>
            _relativeUrlStartsWith = relativeUrlStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
            _relativeUrlStartsWith.Any(filterUrl => url.PathAndQuery.StartsWithOrdinalIgnoreCase(filterUrl));
    }
}
