using Atata;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Helpers
{
    public static class ReliabilityHelper
    {
        private static readonly Func<Func<bool>, Func<bool>> _retryIfStaleProcess = (innerProcess) => () =>
        {
            try
            {
                return innerProcess();
            }
            catch (StaleElementReferenceException)
            {
                // When navigating away this exception will be thrown for all old element references. Not nice to use
                // exceptions but there doesn't seem to be a better way to do this.
                return false;
            }
        };

        private static readonly Func<Func<bool>, Func<bool>> _retryIfNotStaleProcess = (innerProcess) => () =>
        {
            try
            {
                return innerProcess();
            }
            catch (StaleElementReferenceException)
            {
                return true;
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
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static void DoWithRetriesOrFail(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null)
        {
            if (!DoWithRetries(process, timeout, interval))
            {
                throw new TimeoutException("The process didn't succeed with retries before timing out.");
            }
        }

        /// <summary>
        /// Executes the process repeatedly while it's not successful, with the given timeout and retry intervals.
        /// </summary>
        /// <param name="process">
        /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="process"/> succeeded (regardless of it happening on the first try
        /// or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static bool DoWithRetries(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null)
        {
            var wait = new SafeWait<object>(new object());

            // If no values are supplied then the defaults specified in AtataFactory will be used.
            if (timeout != null) wait.Timeout = timeout.Value;
            if (interval != null) wait.PollingInterval = interval.Value;

            return wait.Until(_ => process());
        }

        /// <summary>
        /// Executes the process and retries if an element becomes stale (<see cref="StaleElementReferenceException"/>).
        /// If the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        ///
        /// In situations like a DataTable load it is possible that the page will change during execution of multiple
        /// long running operations such as GetAll, causing stale virtual DOM. Such change tends to be near
        /// instantaneous and only happens once at a time so this should pass by the 2nd try.
        /// </summary>
        /// <param name="process">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static void RetryIfStaleOrFail(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null) =>
            DoWithRetriesOrFail(_retryIfStaleProcess(process), timeout, interval);

        /// <summary>
        /// Executes the process and retries if an element becomes stale (<see cref="StaleElementReferenceException"/>).
        ///
        /// In situations like a DataTable load it is possible that the page will change during execution of multiple
        /// long running operations such as GetAll, causing stale virtual DOM. Such change tends to be near
        /// instantaneous and only happens once at a time so this should pass by the 2nd try.
        /// </summary>
        /// <param name="process">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="process"/> succeeded (regardless of it happening on the first try
        /// or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static bool RetryIfStale(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null) =>
            DoWithRetries(_retryIfStaleProcess(process), timeout, interval);

        /// <summary>
        /// Executes the process and retries until no element is stale (<see cref="StaleElementReferenceException"/>).
        ///
        /// If the operation didn't succeed then throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="process">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <exception cref="TimeoutException">
        /// Thrown if the operation didn't succeed even after retries within the allotted time.
        /// </exception>
        public static void RetryIfNotStaleOrFail(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null) =>
            DoWithRetriesOrFail(_retryIfNotStaleProcess(process), timeout, interval);

        /// <summary>
        /// Executes the process and retries until no element is stale (<see cref="StaleElementReferenceException"/>).
        /// </summary>
        /// <param name="process">
        /// The long running operation that may execute during DOM change and should be retried. Should return <see
        /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="timeout">The maximum time allowed for the process to complete.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="process"/> succeeded (regardless of it happening on the first try
        /// or during retries, <see langword="false"/> otherwise.
        /// </returns>
        public static bool RetryIfNotStale(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null) =>
            DoWithRetries(_retryIfNotStaleProcess(process), timeout, interval);
    }
}
