using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ReliabilityUITestContextExtensions
    {
        /// <summary>
        /// Trying to execute an operation until the given element exists.
        /// </summary>
        /// <param name="process">Operation to execute.</param>
        /// <param name="elementToWaitFor">Selector of the element that's required to exist.</param>
        /// <param name="timeout">Timeout of the operation.</param>
        /// <param name="interval">Time between retries.</param>
        /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
        public static void DoWithRetriesUntilExists(
            this UITestContext context,
            Action process,
            By elementToWaitFor,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            TimeSpan? existsTimeout = null) =>
            ReliabilityHelper.DoWithRetries(
                () =>
                {
                    process();

                    // The timeout for this existence check needs to be significantly smaller than the timeout of the
                    // whole retry logic so actually multiple tries can happen.
                    existsTimeout ??= (timeout ?? TimeoutConfiguration.Default.RetryTimeout) / 5;

                    return context.Exists(elementToWaitFor.Safely().Within(
                        existsTimeout.Value,
                        interval ?? TimeoutConfiguration.Default.RetryInterval));
                },
                timeout,
                interval);

        /// <summary>
        /// Trying to execute an operation until the given element goes missing.
        /// </summary>
        /// <param name="process">Operation to execute.</param>
        /// <param name="elementToWaitForGoMissing">Selector of the element that's required to go missing.</param>
        /// <param name="timeout">Timeout of the operation.</param>
        /// <param name="interval">Time between retries.</param>
        /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
        public static void DoWithRetriesUntilMissing(
            this UITestContext context,
            Action process,
            By elementToWaitForGoMissing,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            TimeSpan? existsTimeout = null) =>
            ReliabilityHelper.DoWithRetries(
                () =>
                {
                    process();

                    // The timeout for this existence check needs to be significantly smaller than the timeout of the
                    // whole retry logic so actually multiple tries can happen.
                    existsTimeout ??= (timeout ?? TimeoutConfiguration.Default.RetryTimeout) / 5;

                    return context.Missing(elementToWaitForGoMissing.Safely().Within(
                        existsTimeout.Value,
                        interval ?? TimeoutConfiguration.Default.RetryInterval));
                },
                timeout,
                interval);
    }
}
