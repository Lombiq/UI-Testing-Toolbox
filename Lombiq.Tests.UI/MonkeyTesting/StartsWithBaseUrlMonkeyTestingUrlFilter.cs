using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class StartsWithBaseUrlMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        public bool AllowUrl(UITestContext context, Uri url) =>
            url.AbsoluteUri.StartsWith(context.Scope.BaseUri.AbsoluteUri, StringComparison.Ordinal);
    }
}
