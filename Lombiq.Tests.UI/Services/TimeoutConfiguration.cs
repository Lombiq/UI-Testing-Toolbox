using System;

namespace Lombiq.Tests.UI.Services
{
    public class TimeoutConfiguration
    {
        /// <summary>
        /// Gets or sets how long to wait for an operation to finish when it's retried.
        /// </summary>
        public TimeSpan RetryTimeout { get; set; }

        /// <summary>
        /// Gets or sets how long to wait between retries after the operation didn't complete successfully. Note that
        /// this is included in <see cref="RetryTimeout"/>.
        /// </summary>
        /// <example>
        /// Something permanently failing will be checked for a total of 10s with a <see cref="RetryTimeout"/> of 10s
        /// and <see cref="RetryInterval"/> of 5s, just then there will be two-three checks altogether (first check
        /// fails, wait 5s, second check fails, wait 5s, and if we're within 10s still, a third check will also fail).
        /// </example>
        public TimeSpan RetryInterval { get; set; }

        /// <summary>
        /// Gets or sets how long to wait for a page load to finish. Note that this is mostly for the initial page load
        /// of an app (i.e. app start), running the Orchard setup and importing large recipes.
        /// </summary>
        public TimeSpan PageLoadTimeout { get; set; }


        public static readonly TimeoutConfiguration Default = new TimeoutConfiguration
        {
            RetryTimeout = TimeSpan
                .FromSeconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.RetryTimeoutSeconds", 15)),
            RetryInterval = TimeSpan
                .FromMilliseconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.RetryIntervalMillisecondSeconds", 500)),
            PageLoadTimeout = TimeSpan
                .FromSeconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.PageLoadTimeoutSeconds", 180)),
        };
    }
}
