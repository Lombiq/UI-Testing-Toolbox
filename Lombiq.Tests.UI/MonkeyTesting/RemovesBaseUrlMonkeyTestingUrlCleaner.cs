using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class RemovesBaseUrlMonkeyTestingUrlCleaner : IMonkeyTestingUrlCleaner
    {
        public Uri Clean(UITestContext context, Uri url)
        {
            string baseUrl = context.Scope.BaseUri.AbsoluteUri;
            string urlAsString = url.OriginalString;

            if (!string.IsNullOrEmpty(baseUrl) && urlAsString.StartsWith(baseUrl, StringComparison.Ordinal))
            {
                urlAsString = urlAsString[baseUrl.Length..];
                if (!urlAsString.StartsWith('/')) urlAsString = '/' + urlAsString;
                return new Uri(urlAsString, UriKind.RelativeOrAbsolute);
            }

            return url;
        }
    }
}
