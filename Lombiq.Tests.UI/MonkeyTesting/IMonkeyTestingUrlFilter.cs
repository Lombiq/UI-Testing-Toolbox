using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// An URL filter that is used in monkey testing.
    /// </summary>
    public interface IMonkeyTestingUrlFilter
    {
        /// <summary>
        /// Determines whether this filter allows the specified URL to be tested.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The URL.</param>
        /// <returns><see langword="true"/> if URL passes the filter; otherwise, <see langword="false"/>.</returns>
        bool AllowUrl(UITestContext context, Uri url);
    }
}
