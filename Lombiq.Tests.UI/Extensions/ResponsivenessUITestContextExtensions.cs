using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using System.Drawing;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Extensions;

public static class ResponsivenessUITestContextExtensions
{
    /// <summary>
    /// Sets the browser's resolution to <see cref="CommonDisplayResolutions.Standard"/>.
    /// </summary>
    public static void SetStandardBrowserSize(this UITestContext context) =>
        context.SetBrowserSize(CommonDisplayResolutions.Standard);

    /// <summary>
    /// Sets the browser's resolution to <see cref="BrowserConfiguration.DefaultBrowserSize"/>.
    /// </summary>
    public static void SetDefaultBrowserSize(this UITestContext context) =>
        context.SetBrowserSize(context.Configuration.BrowserConfiguration.DefaultBrowserSize);

    /// <summary>
    /// Set the browser window's size to the given value. See <see cref="CommonDisplayResolutions"/> for
    /// some resolution presets (but generally it's better to test the given app's responsive breakpoints
    /// specifically).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that if you switch windows/tabs during the test you may need to set the browser size again.
    /// </para>
    /// </remarks>
    /// <param name="size">The outer size of the browser window.</param>
    public static void SetBrowserSize(this UITestContext context, Size size)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
            "Set window size to {0}x{1}.", size.Width, size.Height);
        context.Driver.Manage().Window.Size = size;
    }
}
