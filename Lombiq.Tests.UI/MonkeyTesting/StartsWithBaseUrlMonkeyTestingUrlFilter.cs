using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class StartsWithBaseUrlMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        public bool CanHandle(string url, UITestContext context) =>
            url.StartsWith(context.Scope.BaseUri.AbsoluteUri, StringComparison.Ordinal);
    }
}
