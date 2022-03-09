using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlSanitizers
{
    /// <summary>
    /// Represents the URL sanitizer that removes an <see cref="UITestContext"/> base URL part if it is present.
    /// </summary>
    public sealed class RemovesBaseUrlMonkeyTestingUrlSanitizer : IMonkeyTestingUrlSanitizer
    {
        public Uri Sanitize(UITestContext context, Uri url)
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
