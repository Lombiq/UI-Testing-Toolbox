using System;

namespace Lombiq.Tests.UI.Models
{
    /// <summary>
    /// Represents a state of some web content that may change, for example via page navigation or dynamic content after
    /// user interaction.
    /// </summary>
    public interface IWebContentState
    {
        /// <summary>
        /// Returns <see langword="true"/> if navigation has occurred or the content has changed based on some
        /// previously provided content.
        /// </summary>
        bool CheckIfNavigationHasOccurred();

        /// <summary>
        /// Waits until <see cref="CheckIfNavigationHasOccurred"/> evaluates to <see langword="true"/>.
        /// </summary>
        public void Wait(TimeSpan? timeout = null, TimeSpan? interval = null);
    }
}
