using Lombiq.Tests.UI.Services;
using System;
namespace Lombiq.Tests.UI.MonkeyTesting
{
    public class StartsWithMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly string _relativeUrlStartsWith;

        public StartsWithMonkeyTestingUrlFilter(string relativeUrlStartsWith) =>
            _relativeUrlStartsWith = relativeUrlStartsWith;

        public bool AllowUrl(UITestContext context, Uri url) =>
            url.PathAndQuery.StartsWithOrdinalIgnoreCase(_relativeUrlStartsWith);
    }
}
