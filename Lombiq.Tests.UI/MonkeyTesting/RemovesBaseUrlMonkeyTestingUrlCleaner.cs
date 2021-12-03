using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class RemovesBaseUrlMonkeyTestingUrlCleaner : IMonkeyTestingUrlCleaner
    {
        public string Handle(string url, UITestContext context)
        {
            string baseUrl = context.Scope.BaseUri.AbsoluteUri;

            if (!string.IsNullOrEmpty(baseUrl) && url.StartsWith(baseUrl, StringComparison.Ordinal))
            {
                url = url[baseUrl.Length..];
                return !url.StartsWith('/')
                    ? '/' + url
                    : url;
            }

            return url;
        }
    }
}
