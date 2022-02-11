using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if NOT starts with the configured relative URL.
    /// </summary>
    [DebuggerDisplay("Starts with {_relativeUrlNotStartsWith}")]
    public class NotStartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string _relativeUrlNotStartsWith;

        public NotStartsWithMonkeyTestingUrlFilter(string relativeUrlNotStartsWith) =>
            _relativeUrlNotStartsWith = relativeUrlNotStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
            !url.PathAndQuery.StartsWithOrdinalIgnoreCase(_relativeUrlNotStartsWith);
    }
}
