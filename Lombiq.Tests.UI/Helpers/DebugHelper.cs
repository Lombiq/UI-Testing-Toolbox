using System;
using System.Diagnostics;
using System.Globalization;

namespace Lombiq.Tests.UI.Helpers
{
    public static class DebugHelper
    {
        public static void WriteLineTimestamped(string format, params object[] args) =>
            Debug.WriteLine(PrefixWithTimestamp(format), args);

        public static string PrefixWithTimestamp(string message) =>
            $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} - {message}";
    }
}
