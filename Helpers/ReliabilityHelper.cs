using Lombiq.Tests.UI.Services;
using System;
using System.Threading;

namespace Lombiq.Tests.UI.Helpers
{
    public static class ReliabilityHelper
    {
        public static void DoWithRetries(Func<bool> process, TimeSpan? timeout = null)
        {
            if (timeout == null) timeout = TimeoutConfiguration.Default.RetryTimeout;

            // Did I ever tell you what the definition of insanity is?
            SpinWait.SpinUntil(process, timeout.Value);
        }
    }
}
