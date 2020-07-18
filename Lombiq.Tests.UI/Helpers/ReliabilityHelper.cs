using Atata;
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
    }
}
