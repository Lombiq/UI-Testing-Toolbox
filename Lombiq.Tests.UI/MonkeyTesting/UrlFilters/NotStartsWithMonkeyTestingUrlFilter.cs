using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics;
using System.Linq;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if NOT starts with the configured relative URL(s).
    /// </summary>
    [DebuggerDisplay("Does NOT start with {_relativeUrlNotStartsWith}")]
    public class NotStartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string[] _relativeUrlNotStartsWith;

        public NotStartsWithMonkeyTestingUrlFilter(params string[] relativeUrlNotStartsWith) =>
            _relativeUrlNotStartsWith = relativeUrlNotStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
           !_relativeUrlNotStartsWith.Any(filterUrl => url.PathAndQuery.StartsWithOrdinalIgnoreCase(filterUrl));
    }
}
