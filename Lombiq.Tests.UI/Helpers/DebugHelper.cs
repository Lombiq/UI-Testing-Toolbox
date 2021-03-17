using System;
using System.Diagnostics;
using System.Globalization;

namespace Lombiq.Tests.UI.Helpers
{
    public static class DebugHelper
    {
        public static void WriteLineTimestamped(string format, params object[] args)
        {
            // Preventing "FormatException : Input string was not in a correct format." exceptions if the message
            // contains characters used in string formatting but it shouldn't actually be formatted.
            if (args == null || args.Length == 0) Debug.WriteLine(PrefixWithTimestamp(format));
            else Debug.WriteLine(PrefixWithTimestamp(format), args);
        }

        // Note that this uses UTC, while Atata's log uses the local time zone:
        // https://github.com/atata-framework/atata/issues/483.
        public static string PrefixWithTimestamp(string message) =>
            $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} - {message}";
    }
}
