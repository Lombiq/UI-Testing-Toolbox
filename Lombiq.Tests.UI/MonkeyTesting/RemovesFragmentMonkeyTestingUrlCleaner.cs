using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class RemovesFragmentMonkeyTestingUrlCleaner : IMonkeyTestingUrlCleaner
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
