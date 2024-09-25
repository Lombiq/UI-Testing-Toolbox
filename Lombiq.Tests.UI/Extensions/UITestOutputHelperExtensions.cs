using CliWrap;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Extensions;

public static class UITestOutputHelperExtensions
{
    /// <summary>
    /// Creates a new delegate pipe target that calls <see
    /// cref="TestOutputHelperExtensions.WriteOutputTimestampedAndDebug"/>.
    /// </summary>
    public static PipeTarget ToPipeTarget(this ITestOutputHelper testOutputHelper, string name) =>
        PipeTarget.ToDelegate(line => testOutputHelper.WriteOutputTimestampedAndDebug(name, line));
}
