using Atata;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Helpers
{
    public static class ReliabilityHelper
    {
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
            DoWithRetries(
                () =>
                {
                    try
                    {
                        return process();
                    }
                    catch (StaleElementReferenceException)
                    {
                        // When navigating away this exception will be thrown for all old element references. Not nice
                        // to use exceptions but there doesn't seem to be a better way to do this.
                        return false;
                    }
                },
                timeout,
                interval);

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
            DoWithRetries(
                () =>
                {
                    try
                    {
                        return process();
                    }
                    catch (StaleElementReferenceException)
                    {
                        return true;
                    }
                },
                timeout,
                interval);
    }
}
