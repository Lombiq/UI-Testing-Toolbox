using System;

namespace Lombiq.Tests.UI.Services
{
    public class TimeoutConfiguration
    {
        /// <summary>
        /// How long to wait for an operation to finish when it's retried.
        /// </summary>
        public TimeSpan RetryTimeout { get; set; }

        /// <summary>
        /// How long to wait between retries after the operation didn't complete successfully. Note that this is
        /// included in <see cref="RetryTimeout"/>, e.g. something permanently failing will be checked for a total of
        /// 10s with a <see cref="RetryTimeout"/> of 5s, just then there will be two checks altogether.
        /// </summary>
        public TimeSpan RetryInterval { get; set; }

        /// <summary>
        /// How long to wait for a page load to finish. Note that this is mostly for the initial page load of an app
        /// (i.e. app start), running the Orchard setup and importing large recipes.
        /// </summary>
        public TimeSpan PageLoadTimeout { get; set; }


        public static readonly TimeoutConfiguration Default = new TimeoutConfiguration
        {
            RetryTimeout = TimeSpan
                .FromSeconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.RetryTimeoutSeconds") ?? 60),
            RetryInterval = TimeSpan
                .FromSeconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.RetryIntervalSeconds") ?? 60),
            PageLoadTimeout = TimeSpan
                .FromSeconds(TestConfigurationManager.GetIntConfiguration("TimeoutConfiguration.PageLoadTimeoutSeconds") ?? 180)
        };
    }
}
