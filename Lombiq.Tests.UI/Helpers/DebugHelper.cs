using System;
using System.Diagnostics;

namespace Lombiq.Tests.UI.Helpers
{
    public static class DebugHelper
    {
        public static void WriteTimestampedLine(string message) => Debug.WriteLine(DateTime.Now + " - " + message);
    }
}
