using Lombiq.Tests.UI.Helpers;

namespace Xunit.Abstractions;

public static class TestOutputHelperExtensions
{
    public static void WriteLineTimestampedAndDebug(this ITestOutputHelper testOutputHelper, string format, params object[] args)
    {
        testOutputHelper.WriteLineTimestamped(format, args);
        DebugHelper.WriteLineTimestamped(format, args);
    }

    public static void WriteLineTimestamped(this ITestOutputHelper testOutputHelper, string format, params object[] args)
    {
        // Preventing "FormatException : Input string was not in a correct format." exceptions if the message
        // contains characters used in string formatting but it shouldn't actually be formatted.
        if (args == null || args.Length == 0) testOutputHelper.WriteLine(DebugHelper.PrefixWithTimestamp(format));
        else testOutputHelper.WriteLine(DebugHelper.PrefixWithTimestamp(format), args);
    }
}
