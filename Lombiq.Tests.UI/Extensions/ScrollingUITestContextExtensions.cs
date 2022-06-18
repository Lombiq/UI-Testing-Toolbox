using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;

namespace Lombiq.Tests.UI.Extensions;

public static class ScrollingUITestContextExtensions
{
    /// <summary>
    /// Scrolls to a particular set of coordinates in the document.
    /// </summary>
    public static void ScrollTo(this UITestContext context, Point position) =>
        context.ExecuteScript($"window.scrollTo(arguments[0], arguments[1], \"instant\");", position.X, position.Y);

    /// <summary>
    /// Scrolls the document vertically to the given <paramref name="position"/>.
    /// </summary>
    public static void ScrollVertical(this UITestContext context, int position)
    {
        var currentPosition = context.GetScrollPosition();

        context.ScrollTo(new Point(currentPosition.Y, position));
    }

    /// <summary>
    /// Scrolls the document horizontally to the given <paramref name="position"/>.
    /// </summary>
    public static void ScrollHorizontal(this UITestContext context, int position)
    {
        var currentPosition = context.GetScrollPosition();

        context.ScrollTo(new Point(position, currentPosition.Y));
    }

    /// <summary>
    /// Gets the current scroll position.
    /// </summary>
    public static Point GetScrollPosition(this UITestContext context)
    {
        var position = (ReadOnlyCollection<object>)context.Driver.ExecuteScript(
            "return [window.scrollX, window.scrollY];");

        return new Point(
            Convert.ToInt32(position[0], CultureInfo.InvariantCulture),
            Convert.ToInt32(position[1], CultureInfo.InvariantCulture));
    }

    public static bool GetIsScrollBarHidden(this UITestContext context)
    {
        var scrollBarState = @"
    if (!window.$Lombiq_Tests_UI_Scrolling_Context) {
        return false;
    }

    return !!window.$Lombiq_Tests_UI_Scrolling_Context.hidden;
";

        return (bool)context.Driver.ExecuteScript(scrollBarState);
    }

    public static void HideScrollBar(this UITestContext context)
    {
        var hideScrollBar = @"
    if (!window.$Lombiq_Tests_UI_Scrolling_Context) {
        window.$Lombiq_Tests_UI_Scrolling_Context = {
            original: {
                style: {
                    overflow: document.body.style.overflow,
                },
            },
        };
    }
    if (!window.$Lombiq_Tests_UI_Scrolling_Context.hidden) {
        document.body.style.overflow = 'hidden';
        window.$Lombiq_Tests_UI_Scrolling_Context.hidden = true;
    }
";
        context.Driver.ExecuteScript(hideScrollBar);
    }

    public static void RestoreHiddenScrollBar(this UITestContext context)
    {
        var showScrollBar = @"
    if (!window.$Lombiq_Tests_UI_Scrolling_Context || !window.$Lombiq_Tests_UI_Scrolling_Context.hidden) {
        return;
    }
    document.body.style.overflow = window.$Lombiq_Tests_UI_Scrolling_Context.original.style.overflow;
";
        context.Driver.ExecuteScript(showScrollBar);
    }
}
