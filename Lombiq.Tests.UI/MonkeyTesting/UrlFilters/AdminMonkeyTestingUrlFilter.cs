using Lombiq.Tests.UI.Services;
using System;
namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if it's an admin page (i.e. an URL under /admin).
    /// </summary>
    public class AdminMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly StartsWithMonkeyTestingUrlFilter _startsWithMonkeyTestingUrlFilter = new("/admin");

        public bool AllowUrl(UITestContext context, Uri url) => _startsWithMonkeyTestingUrlFilter.AllowUrl(context, url);
    }
}
