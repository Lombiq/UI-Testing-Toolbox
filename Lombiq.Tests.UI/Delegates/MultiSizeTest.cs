using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Delegates
{
    /// <summary>
    /// An test action where the window is sized to one of the two screen sizes in <see cref="OrchardCoreUITestBase"/>.
    /// </summary>
    /// <param name="context">The context of the currently executed UI test.</param>
    /// <param name="isStandardSize">
    /// A value indicating the screen size being used. If <see langword="true" /> then the window is set to <see
    /// cref="OrchardCoreUITestBase.StandardBrowserSize"/>, otherwise to <see
    /// cref="OrchardCoreUITestBase.MobileBrowserSize"/>.
    /// </param>
    public delegate void MultiSizeTest(UITestContext context, bool isStandardSize);

    /// <inheritdoc cref="MultiSizeTest"/>
    public delegate Task MultiSizeTestAsync(UITestContext context, bool isStandardSize);
}
