using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ReliabilityUITestContextExtensions
    {
        /// <summary>
        /// Executes the process repeatedly while it's not successful, with the given timeout and retry intervals. If
        /// the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="processAsync">
        /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static Task DoWithRetriesOrFailAsync(
            this UITestContext context,
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.DoWithRetriesOrFailAsync(
                processAsync,
                timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
                interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

        /// <summary>
        /// Executes the process and retries if an element becomes stale (<see cref="StaleElementReferenceException"/>).
        /// If the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        ///
        /// In situations like a DataTable load it is possible that the page will change during execution of multiple
        /// long running operations such as GetAll, causing stale virtual DOM. Such change tends to be near
        /// instantaneous and only happens once at a time so this should pass by the 2nd try.
        /// </summary>
        /// <param name="processAsync">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static Task RetryIfStaleOrFailAsync(
            this UITestContext context,
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.RetryIfStaleOrFailAsync(
                processAsync,
                timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
                interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

        /// <summary>
        /// Executes the process and retries until no element is stale (<see cref="StaleElementReferenceException"/>).
        ///
        /// If the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="processAsync">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to <paramref
        /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static Task RetryIfNotStaleOrFailAsync(
            this UITestContext context,
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.RetryIfNotStaleOrFailAsync(
                processAsync,
                timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
                interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

        /// <summary>
        /// Tries to execute an operation until the given element exists.
        /// </summary>
        /// <param name="processAsync">Operation to execute.</param>
        /// <param name="elementToWaitFor">Selector of the element that's required to exist.</param>
        /// <param name="timeout">Timeout of the operation.</param>
        /// <param name="interval">Time between retries.</param>
        /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
        public static Task DoWithRetriesUntilExistsAsync(
            this UITestContext context,
            Func<Task> processAsync,
            By elementToWaitFor,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            TimeSpan? existsTimeout = null) =>
            context.DoWithRetriesOrFailAsync(
                async () =>
                {
                    await processAsync();

                    existsTimeout ??= GetExistsTimeout(context, timeout);

                    return ExistsWithin(context, elementToWaitFor, existsTimeout.Value, interval);
                },
                timeout,
                interval);

        /// <summary>
        /// Tries to execute an operation until the given element goes missing.
        /// </summary>
        /// <param name="processAsync">Operation to execute.</param>
        /// <param name="elementToWaitForGoMissing">Selector of the element that's required to go missing.</param>
        /// <param name="timeout">Timeout of the operation.</param>
        /// <param name="interval">Time between retries.</param>
        /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
        public static Task DoWithRetriesUntilMissingAsync(
            this UITestContext context,
            Func<Task> processAsync,
            By elementToWaitForGoMissing,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            TimeSpan? existsTimeout = null) =>
            context.DoWithRetriesOrFailAsync(
                async () =>
                {
                    await processAsync();

                    existsTimeout ??= GetExistsTimeout(context, timeout);

                    return MissingWithin(context, elementToWaitForGoMissing, existsTimeout.Value, interval);
                },
                timeout,
                interval);

        private static TimeSpan GetExistsTimeout(UITestContext context, TimeSpan? timeout) =>
            // The timeout for this existence check needs to be significantly smaller than the timeout of the
            // whole retry logic so actually multiple tries can happen.
            (timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout) / 5;

        private static bool ExistsWithin(
            UITestContext context,
            By elementToWaitFor,
            TimeSpan existsTimeout,
            TimeSpan? interval = null) =>
            context.Exists(elementToWaitFor.Safely().Within(
                existsTimeout,
                interval ?? context.Configuration.TimeoutConfiguration.RetryInterval));

        private static bool MissingWithin(
            UITestContext context,
            By elementToWaitForGoMissing,
            TimeSpan existsTimeout,
            TimeSpan? interval = null) =>
            context.Missing(elementToWaitForGoMissing.Safely().Within(
                existsTimeout,
                interval ?? context.Configuration.TimeoutConfiguration.RetryInterval));
    }
}
