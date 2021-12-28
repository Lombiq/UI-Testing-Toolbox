using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// An URL sanitizer that is used in monkey testing.
    /// </summary>
    public interface IMonkeyTestingUrlSanitizer
    {
        /// <summary>
        /// Sanitizes the specified URL.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The URL.</param>
        /// <returns>A sanitized or original URL.</returns>
        Uri Sanitize(UITestContext context, Uri url);
    }
}
