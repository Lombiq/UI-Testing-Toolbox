using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class RemovesFragmentMonkeyTestingUrlCleaner : IMonkeyTestingUrlCleaner
    {
        public string Handle(string url, UITestContext context) =>
            new Uri(url, UriKind.RelativeOrAbsolute)
                .GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
    }
}
