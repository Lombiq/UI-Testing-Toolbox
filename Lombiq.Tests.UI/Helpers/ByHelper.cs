using Atata;
using OpenQA.Selenium;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Lombiq.Tests.UI.Helpers;

/// <summary>
/// Provides helper functions for generating <see cref="By"/> selectors.
/// </summary>
public static class ByHelper
{
    /// <summary>
    /// Returns an XPath selector for an email in the list whose headers contain the text <paramref name="text"/>.
    /// </summary>
    public static By SmtpInboxRow(string text) =>
        By
            .XPath($"//tr[contains(@class,'el-table__row')]//div[contains(@class,'cell')][contains(text(), {JsonSerializer.Serialize(text)})]")
            .Within(TimeSpan.FromMinutes(2));

    /// <summary>
    /// Returns an XPath selector that looks up elements with exactly matching <paramref name="innerText"/> and optional
    /// element name restriction.
    /// </summary>
    public static By Text(string innerText, string element = "*") =>
        By.XPath($"//{element}[normalize-space(.) = {JsonSerializer.Serialize(innerText)}]");

    /// <summary>
    /// Same as <see cref="Text"/> but for <c>button</c> element.
    /// </summary>
    public static By ButtonText(string innerText) => Text(innerText, "button");

    /// <summary>
    /// Returns an XPath selector that looks up elements whose text contains <paramref name="innerText"/> with optional
    /// element name restriction.
    /// </summary>
    public static By TextContains(string innerText, string element = "*") =>
        By.XPath($"//{element}[contains(., {JsonSerializer.Serialize(innerText)})]");

    /// <summary>
    /// Creates a <see langword="string"/> from an interpolated string with the invariant culture. This prevents culture-
    /// sensitive formatting of interpolated values.
    /// </summary>
    public static By Css(this DefaultInterpolatedStringHandler value) =>
        By.CssSelector(string.Create(CultureInfo.InvariantCulture, ref value));

    /// <summary>
    /// Returns a CSS selector that looks up a content picker field.
    /// </summary>
    /// <param name="part">The name of the content part.</param>
    /// <param name="field">The name of the content picker field.</param>
    public static By GetContentPickerSelector(string part, string field) =>
        Css($"*[data-part='{part}'][data-field='{field}']");
}
