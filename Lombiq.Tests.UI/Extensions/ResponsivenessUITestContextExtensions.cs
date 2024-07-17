using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using SixLabors.ImageSharp;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
    /// Set the browser window's size to the given value. See <see cref="CommonDisplayResolutions"/> for some resolution
    /// presets (but generally it's better to test the given app's responsive breakpoints specifically).
    /// </summary>
    /// <remarks>
    /// <para>Note that if you switch windows/tabs during the test you may need to set the browser size again.</para>
    /// </remarks>
    /// <param name="size">The outer size of the browser window.</param>
    public static void SetBrowserSize(this UITestContext context, Size size)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
            "Set window size to {0}x{1}.", size.Width, size.Height);
        context.Driver.Manage().Window.Size = new System.Drawing.Size(size.Width, size.Height);
        context.ExecuteScript("document.body.style.transform = 'scale(1)';");
        context.ExecuteScript("document.body.style.transform = '';");
    }

    /// <summary>
    /// Set the browser inner size to the given value. See <see cref="CommonDisplayResolutions"/> for some resolution
    /// presets (but generally it's better to test the given app's responsive breakpoints specifically).
    /// </summary>
    /// <remarks>
    /// <para>Note that if you switch windows/tabs during the test you may need to set the browser size again.</para>
    /// </remarks>
    /// <param name="size">The inner size of the browser window.</param>
    public static void SetViewportSize(this UITestContext context, Size size)
    {
        var getPadding = @"return [ window.outerWidth - window.innerWidth, window.outerHeight - window.innerHeight ];";
        var paddings = context.ExecuteScript(getPadding) as ReadOnlyCollection<object>;

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
            "Calculated paddings {0}x{1}.",
            Convert.ToInt32(paddings[0], CultureInfo.InvariantCulture),
            Convert.ToInt32(paddings[1], CultureInfo.InvariantCulture));

        context.SetBrowserSize(new Size
        {
            Width = size.Width + Convert.ToInt32(paddings[0], CultureInfo.InvariantCulture),
            Height = size.Height + Convert.ToInt32(paddings[1], CultureInfo.InvariantCulture),
        });
    }

    /// <summary>
    /// Gets the inner size of the browser window.
    /// </summary>
    public static Size GetViewportSize(this UITestContext context)
    {
        var innerSize = (ReadOnlyCollection<object>)context.ExecuteScript(
            "return [window.innerWidth, window.innerHeight];");

        return new Size(
            Convert.ToInt32(innerSize[0], CultureInfo.InvariantCulture),
            Convert.ToInt32(innerSize[1], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Gets the client size of the element.
    /// </summary>
    public static Size GetClientSize(this UITestContext context, IWebElement element)
    {
        var clientSize = (ReadOnlyCollection<object>)context.ExecuteScript(
            "return [arguments[0].clientWidth, arguments[0].clientHeight];", element);

        return new Size(
            Convert.ToInt32(clientSize[0], CultureInfo.InvariantCulture),
            Convert.ToInt32(clientSize[1], CultureInfo.InvariantCulture));
    }
}
