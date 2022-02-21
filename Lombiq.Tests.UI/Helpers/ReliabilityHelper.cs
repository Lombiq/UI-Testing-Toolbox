using Atata;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Helpers
{
    public static class ReliabilityHelper
    {
        private static readonly Func<Func<Task<bool>>, Func<Task<bool>>> _retryIfStaleProcess = innerProcess => () =>
        {
            try
            {
                return innerProcess();
            }
            catch (StaleElementReferenceException)
            {
                // When navigating away this exception will be thrown for all old element references. Not nice to use
                // exceptions but there doesn't seem to be a better way to do this.
                return Task.FromResult(false);
            }
        };

        private static readonly Func<Func<Task<bool>>, Func<Task<bool>>> _retryIfNotStaleProcess = innerProcess => () =>
        {
            try
            {
                return innerProcess();
            }
            catch (StaleElementReferenceException)
            {
                return Task.FromResult(true);
            }
        };

        /// <summary>
        /// Executes the process repeatedly while it's not successful, with the given timeout and retry intervals. If
        /// the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="process">
        /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static void DoWithRetriesOrFail(
            Func<bool> process,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var result = DoWithRetriesInternal(process, timeout, interval);
            if (!result.IsSuccess)
            {
                throw new TimeoutException(
                    "The process didn't succeed with retries before timing out " +
                    $"(timeout: {result.Wait.Timeout}, polling interval: {result.Wait.PollingInterval}).");
            }
        }

        /// <summary>
        /// Executes the async process repeatedly while it's not successful, with the given timeout and retry intervals. If
        /// the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="processAsync">
        /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static async Task DoWithRetriesOrFailAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var result = await DoWithRetriesInternalAsync(processAsync, timeout, interval);
            if (!result.IsSuccess)
            {
                throw new TimeoutException(
                    "The process didn't succeed with retries before timing out " +
                    $"(timeout: {result.Wait.Timeout}, polling interval: {result.Wait.PollingInterval}).");
            }
        }

        /// <summary>
        /// Executes the process repeatedly while it's not successful, with the given timeout and retry intervals.
        /// </summary>
        /// <param name="processAsync">
        /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="processAsync"/> succeeded (regardless of it happening on the first
        /// try or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static async Task<bool> DoWithRetriesAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
                (await DoWithRetriesInternalAsync(processAsync, timeout, interval)).IsSuccess;

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
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static Task RetryIfStaleOrFailAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
                DoWithRetriesOrFailAsync(_retryIfStaleProcess(processAsync), timeout, interval);

        /// <summary>
        /// Executes the process and retries if an element becomes stale (<see cref="StaleElementReferenceException"/>).
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
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="processAsync"/> succeeded (regardless of it happening on the first
        /// try or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static Task<bool> RetryIfStaleAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
                DoWithRetriesAsync(_retryIfStaleProcess(processAsync), timeout, interval);

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
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static Task RetryIfNotStaleOrFailAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
                DoWithRetriesOrFailAsync(_retryIfNotStaleProcess(processAsync), timeout, interval);

        /// <summary>
        /// Executes the process and retries until no element is stale (<see cref="StaleElementReferenceException"/>).
        /// </summary>
        /// <param name="processAsync">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">
        /// The maximum time allowed for the process to complete. Defaults to the default of <see
        /// cref="SafeWait{T}.Timeout"/>.
        /// </param>
        /// <param name="interval">
        /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to the default of <see
        /// cref="SafeWait{T}.PollingInterval"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="processAsync"/> succeeded (regardless of it happening on the first try
        /// or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static Task<bool> RetryIfNotStaleAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
                DoWithRetriesAsync(_retryIfNotStaleProcess(processAsync), timeout, interval);

        private static (bool IsSuccess, SafeWait<object> Wait) DoWithRetriesInternal(
            Func<bool> process,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var wait = new SafeWait<object>(new object());

            // If no values are supplied then the defaults specified in AtataFactory will be used.
            if (timeout != null) wait.Timeout = timeout.Value;
            if (interval != null) wait.PollingInterval = interval.Value;

            return (wait.Until(_ => process()), wait);
        }

        private static async Task<(bool IsSuccess, SafeWait<object> Wait)> DoWithRetriesInternalAsync(
            Func<Task<bool>> processAsync,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var wait = new SafeWait<object>(new object());

            // If no values are supplied then the defaults specified in AtataFactory will be used.
            if (timeout != null) wait.Timeout = timeout.Value;
            if (interval != null) wait.PollingInterval = interval.Value;

            return (await processAsync(), wait);
        }
    }
}
