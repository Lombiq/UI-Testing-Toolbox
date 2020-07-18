using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ReliabilityUITestContextExtensions
    {
        public static void DoWithRetriesUntilExists(
            this UITestContext context,
            Action process,
            By elementToWaitFor,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            TimeSpan? existsTimeout = null) =>
            ReliabilityHelper.DoWithRetries(() =>
            {
                process();

                // The timeout for this existence check needs to be significantly smaller than the timeout of the
                // whole retry logic so actually multiple tries can happen.
                existsTimeout ??= (timeout ?? TimeoutConfiguration.Default.RetryTimeout) / 5;

                return context.Exists(elementToWaitFor.Safely().Within(
                    existsTimeout.Value,
                    timeout ?? TimeoutConfiguration.Default.RetryInterval));
            }, timeout, interval);
    }
}
