using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Helpers
{
    public static class ReliabilityHelper
    {
        public static void DoWithRetries(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null)
        {
            var wait = new SafeWait<object>(new object());

            // If no values are supplied then the defaults specified in AtataFactory will be used.
            if (timeout != null) wait.Timeout = timeout.Value;
            if (interval != null) wait.PollingInterval = interval.Value;

            wait.Until(_ => process());
        }

        /// <summary>
        /// Executes the process and retries if an element becomes stale (<see cref="StaleElementReferenceException"/>).
        ///
        /// In situations like a DataTable load it is possible that the page will change during execution of multiple
        /// long running operations such as GetAll, causing stale virtual DOM. Such change tends to be near
        /// instantaneous and only happens once at a time so this should pass by the 2nd try.
        /// </summary>
        /// <param name="process">The long running operation that may execute during DOM change.</param>
        /// <param name="timeout">The maximum time allotted for retries.</param>
        /// <param name="interval">The polling interval used by <see cref="SafeWait{T}"/>.</param>
        public static void DoUntilNotStale(Func<bool> process, TimeSpan? timeout = null, TimeSpan? interval = null) =>
            DoWithRetries(
                () =>
                {
                    try
                    {
                        return process();
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                },
                timeout,
                interval);
    }
}
