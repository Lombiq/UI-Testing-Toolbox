using Lombiq.Tests.UI.Services;
using System;
namespace Lombiq.Tests.UI.MonkeyTesting.UrlFilters
{
    /// <summary>
    /// URL filter that matches the URL to see if it's NOT an admin page (i.e. an URL NOT under /admin).
    /// </summary>
    public class NotAdminMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly AdminMonkeyTestingUrlFilter _adminMonkeyTestingUrlFilter = new();

        public bool AllowUrl(UITestContext context, Uri url) => !_adminMonkeyTestingUrlFilter.AllowUrl(context, url);
    }
}
