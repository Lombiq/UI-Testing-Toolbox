using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting.UrlSanitizers
{
    /// <summary>
    /// URL sanitizer that removes a fragment part of an URL (i.e. that comes after the hash: #).
    /// </summary>
    public sealed class RemovesFragmentMonkeyTestingUrlSanitizer : IMonkeyTestingUrlSanitizer
    {
        public Uri Sanitize(UITestContext context, Uri url)
        {
            if (!string.IsNullOrEmpty(url.Fragment))
            {
                string processedUrl = url.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);

                return new(processedUrl, UriKind.RelativeOrAbsolute);
            }

            return url;
        }
    }
}
