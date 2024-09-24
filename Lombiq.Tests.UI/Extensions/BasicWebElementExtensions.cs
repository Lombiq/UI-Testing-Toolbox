#nullable enable

namespace OpenQA.Selenium;

// Move this to Lombiq UI Testing Toolbox in dev.
public static class BasicWebElementExtensions
{
    /// <summary>
    /// Returns the text content of the <paramref name="element"/> without surrounding whitespace.
    /// </summary>
    public static string? GetTextTrimmed(this IWebElement element) => element.Text?.Trim();

    /// <summary>
    /// Returns a value indicating whether the boolean attribute called <paramref name="attributeName"/> exists. This
    /// returns <see langword="true"/> even if the value is empty, in accordance to how HTML works (e.g. all of the
    /// following are considered true: <c>&lt;input required&gt;</c>, <c>&lt;input required=""&gt;</c>,
    /// <c>&lt;input required="required"&gt;</c>).
    /// </summary>
    public static bool GetBoolAttribute(this IWebElement element, string attributeName) =>
        element.GetAttribute(attributeName) != null;

    /// <summary>
    /// Returns a value indicating whether the element has the <c>disabled</c> attribute.
    /// </summary>
    public static bool IsDisabled(this IWebElement element) => element.GetBoolAttribute("disabled");
}
