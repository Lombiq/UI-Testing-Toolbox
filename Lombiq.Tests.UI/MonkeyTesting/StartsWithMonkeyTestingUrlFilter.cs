using Lombiq.Tests.UI.Services;
using System;
namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// URL filter that matches the URL to see if starts with the configured relative URL.
    /// </summary>
    public class StartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string _relativeUrlStartsWith;

        public StartsWithMonkeyTestingUrlFilter(string relativeUrlStartsWith) =>
            _relativeUrlStartsWith = relativeUrlStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
            url.PathAndQuery.StartsWithOrdinalIgnoreCase(_relativeUrlStartsWith);
    }
}
