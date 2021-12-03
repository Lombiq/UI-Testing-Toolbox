using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// An URL cleaner that is used in monkey testing.
    /// </summary>
    public interface IMonkeyTestingUrlCleaner
    {
        /// <summary>
        /// Cleans the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="context">The context.</param>
        /// <returns>A cleaned or original URL.</returns>
        string Handle(string url, UITestContext context);
    }
}
