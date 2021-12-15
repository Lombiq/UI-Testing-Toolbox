using Lombiq.Tests.UI.Services;

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
        /// <param name="url">The URL.</param>
        /// <param name="context">The context.</param>
        /// <returns><see langword="true"/> if URL passes the filter; otherwise, <see langword="false"/>.</returns>
        bool CanHandle(string url, UITestContext context);
    }
}
