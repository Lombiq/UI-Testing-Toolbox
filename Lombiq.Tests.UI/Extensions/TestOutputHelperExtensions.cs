using Lombiq.Tests.UI.Helpers;

namespace Xunit.Abstractions
{
    public static class TestOutputHelperExtensions
    {
        public static void WriteLineTimestampedAndDebug(this ITestOutputHelper testOutputHelper, string format, params object[] args)
        {
            testOutputHelper.WriteLineTimestamped(format, args);
            DebugHelper.WriteLineTimestamped(format, args);
        }

        public static void WriteLineTimestamped(this ITestOutputHelper testOutputHelper, string format, params object[] args) =>
            testOutputHelper.WriteLine(DebugHelper.PrefixWithTimestamp(format), args);
    }
}
