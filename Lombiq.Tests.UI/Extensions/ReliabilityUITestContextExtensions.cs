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
        /// Tries to execute an operation until the given element exists.
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
            ReliabilityHelper.DoWithRetriesOrFail(
                () =>
                {
                    process();

                    existsTimeout ??= GetExistsTimeout(timeout);

                    return ExistsWithin(context, elementToWaitFor, existsTimeout.Value, interval);
                },
                timeout,
                interval);

        /// <summary>
        /// Tries to execute an operation until the given element goes missing.
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
            ReliabilityHelper.DoWithRetriesOrFail(
                () =>
                {
                    process();

                    existsTimeout ??= GetExistsTimeout(timeout);

                    return MissingWithin(context, elementToWaitForGoMissing, existsTimeout.Value, interval);
                },
                timeout,
                interval);

        private static TimeSpan GetExistsTimeout(TimeSpan? timeout) =>
            // The timeout for this existence check needs to be significantly smaller than the timeout of the
            // whole retry logic so actually multiple tries can happen.
            (timeout ?? TimeoutConfiguration.Default.RetryTimeout) / 5;

        private static bool ExistsWithin(
            UITestContext context,
            By elementToWaitFor,
            TimeSpan existsTimeout,
            TimeSpan? interval = null) =>
            context.Exists(elementToWaitFor.Safely().Within(
                existsTimeout,
                interval ?? TimeoutConfiguration.Default.RetryInterval));

        private static bool MissingWithin(
            UITestContext context,
            By elementToWaitForGoMissing,
            TimeSpan existsTimeout,
            TimeSpan? interval = null) =>
            context.Missing(elementToWaitForGoMissing.Safely().Within(
                existsTimeout,
                interval ?? TimeoutConfiguration.Default.RetryInterval));
    }
}
