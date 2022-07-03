using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions;

public static class ElementStyleUITestContextExtensions
{
    /// <summary>
    /// Sets the element's inline style.
    /// </summary>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="property">CSS property name.</param>
    /// <param name="value">CSS property value.</param>
    public static void SetElementStyle(this UITestContext context, By elementSelector, string property, string value) =>
        context.ExecuteScript(
            "arguments[0].style[arguments[1]] = arguments[2];",
            context.Get(elementSelector),
            property,
            value);
}
