using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL against the base URL of the application under test, i.e. it disallows leaving
    /// the application.
    /// </summary>
    public sealed class StartsWithBaseUrlMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        public bool AllowUrl(UITestContext context, Uri url) =>
            url.AbsoluteUri.StartsWith(context.Scope.BaseUri.AbsoluteUri, StringComparison.Ordinal);
    }
}
