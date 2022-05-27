using Lombiq.Tests.UI.Services;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Delegates;

/// <summary>
/// An test action where the window is sized to one of the two screen sizes in <see cref="OrchardCoreUITestBase"/>.
/// </summary>
/// <param name="context">The context of the currently executed UI test.</param>
/// <param name="isStandardSize">
/// A value indicating the screen size being used. If <see langword="true"/> then the window is set to <see
/// cref="OrchardCoreUITestBase.StandardBrowserSize"/>, otherwise to <see
/// cref="OrchardCoreUITestBase.MobileBrowserSize"/>.
/// </param>
public delegate void MultiSizeTest(UITestContext context, bool isStandardSize);

/// <inheritdoc cref="MultiSizeTest"/>
public delegate Task MultiSizeTestAsync(UITestContext context, bool isStandardSize);

public static class MultiSizeTestExtensions
{
    [SuppressMessage(
        "Minor Code Smell",
        "S4261:Methods should be named according to their synchronicities",
        Justification =
            "This is a conversion method, and we want to make it explicit that the result has an async signature.")]
    public static MultiSizeTestAsync ToAsync(this MultiSizeTest test) =>
        (context, isStandardSize) =>
        {
            test(context, isStandardSize);
            return Task.CompletedTask;
        };
}
