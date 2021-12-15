using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// Represents the URL sanitizer that removes a fragment part of an URL.
    /// </summary>
    public sealed class RemovesFragmentMonkeyTestingUrlSanitizer : IMonkeyTestingUrlSanitizer
    {
        public Uri Clean(UITestContext context, Uri url)
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
