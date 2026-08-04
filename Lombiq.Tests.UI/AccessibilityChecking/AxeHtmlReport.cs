#nullable enable

using Deque.AxeCore.Commons;
using Deque.AxeCore.Selenium;
using OpenQA.Selenium;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace Lombiq.Tests.UI.AccessibilityChecking;

/// <summary>
/// Findings to include in the generated HTML report.
/// </summary>
[Flags]
public enum AxeReportTypes
{
    Violations = 1,
    Incomplete = 2,
    Inapplicable = 4,
    Passes = 8,
    All = Violations | Incomplete | Inapplicable | Passes,
}

/// <summary>
/// Generates standalone HTML reports from Axe results.
/// </summary>
/// <remarks>
/// <para>
/// Taken from https://github.com/TroyWalshProf/SeleniumAxeHtmlDotnet and updated. Since that project is unmaintained,
/// no need to keep the two versions in sync.
/// </para>
/// <para>
/// Maybe at one point we can have an Orchard Core shape template-based, customizable report:
/// https://github.com/Lombiq/UI-Testing-Toolbox/issues/799.
/// </para>
/// <para>
/// Original license:
///
/// MIT License
///
/// Copyright (c) 2024 Troy Walsh
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
/// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
/// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
/// permit persons to whom the Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
/// Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
/// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
/// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
/// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
/// </para>
/// <para>
/// The legacy <c>TWP.Selenium.Axe.Html</c> API surface is preserved through a compatibility shim, see
/// <c>Lombiq.Tests.UI/AccessibilityChecking/AxeHtmlReportShim.cs</c>.
/// </para>
/// </remarks>
public static class AxeHtmlReport
{
    public static void CreateAxeHtmlReport(this IWebDriver webDriver, string destination) =>
        webDriver.CreateAxeHtmlReport(destination, AxeReportTypes.All);

    public static void CreateAxeHtmlReport(this IWebDriver webDriver, string destination, AxeReportTypes requestedResults)
    {
        var axeBuilder = new AxeBuilder(webDriver);
        webDriver.CreateAxeHtmlReport(axeBuilder.Analyze(), destination, requestedResults);
    }

    public static void CreateAxeHtmlReport(this IWebDriver webDriver, IWebElement context, string destination) =>
        webDriver.CreateAxeHtmlReport(context, destination, AxeReportTypes.All);

    public static void CreateAxeHtmlReport(
        this IWebDriver webDriver,
        IWebElement context,
        string destination,
        AxeReportTypes requestedResults)
    {
        var axeBuilder = new AxeBuilder(webDriver);
        context.CreateAxeHtmlReport(axeBuilder.Analyze(context), destination, requestedResults);
    }

    public static void CreateAxeHtmlReport(this ISearchContext context, AxeResult results, string destination) =>
        context.CreateAxeHtmlReport(results, destination, AxeReportTypes.All);

    public static void CreateAxeHtmlReport(
        this ISearchContext context,
        AxeResult results,
        string destination,
        AxeReportTypes requestedResults)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        if (context is IWrapsElement wrappedElement) context = wrappedElement.WrappedElement;

        var violations = results.Violations ?? [];
        var incomplete = results.Incomplete ?? [];
        var passes = results.Passes ?? [];
        var inapplicable = results.Inapplicable ?? [];

        var violationCount = GetCount(violations);
        var incompleteCount = GetCount(incomplete);
        var passCount = GetCount(passes);
        var inapplicableCount = GetCount(inapplicable);

        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var html = new StringBuilder(64 * 1024);
        html.AppendLine("<!DOCTYPE html>")
            .AppendLine("<html lang=\"en\">")
            .AppendLine("<head>")
            .AppendLine("  <meta charset=\"utf-8\">")
            .AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">")
            .AppendLine("  <title>Accessibility Check</title>")
            .AppendLine("  <style>")
            .AppendLine(GetCss(context))
            .AppendLine("  </style>")
            .AppendLine("</head>")
            .AppendLine("<body>")
            .AppendLine("  <main>")
            .AppendLine("    <h1>Accessibility Check</h1>")
            .AppendLine("    <div id=\"metadata\">")
            .AppendLine("      <div id=\"context\">")
            .AppendLine("        <h3>Context:</h3>")
            .AppendLine("        <div class=\"emOne\" id=\"reportContext\">")
            .AppendLine(GetContextContent(results))
            .AppendLine("        </div>")
            .AppendLine("      </div>")
            .AppendLine("      <div id=\"image\">")
            .AppendLine("        <h3>Image:</h3>")
            .AppendLine("        <img class=\"thumbnail\" id=\"screenshotThumbnail\" alt=\"A screenshot of the page\" width=\"33%\" height=\"auto\">")
            .AppendLine("      </div>")
            .AppendLine("      <div id=\"counts\">")
            .AppendLine("        <h3>Counts:</h3>")
            .AppendLine("        <div class=\"emOne\">")
            .AppendLine(GetCountContent(violationCount, incompleteCount, passCount, inapplicableCount, requestedResults))
            .AppendLine("        </div>")
            .AppendLine("      </div>")
            .AppendLine("    </div>")
            .AppendLine("    <div id=\"results\">");

        if (violationCount > 0 && requestedResults.HasFlag(AxeReportTypes.Violations))
        {
            AppendReadableAxeResults(html, violations, "Violations");
        }

        if (incompleteCount > 0 && requestedResults.HasFlag(AxeReportTypes.Incomplete))
        {
            AppendReadableAxeResults(html, incomplete, "Incomplete");
        }

        if (passCount > 0 && requestedResults.HasFlag(AxeReportTypes.Passes))
        {
            AppendReadableAxeResults(html, passes, "Passes");
        }

        if (inapplicableCount > 0 && requestedResults.HasFlag(AxeReportTypes.Inapplicable))
        {
            AppendReadableAxeResults(html, inapplicable, "Inapplicable");
        }

        html.AppendLine("    </div>")
            .AppendLine("    <div id=\"modal\">")
            .AppendLine("      <div id=\"modalclose\">X</div>")
            .AppendLine("      <img id=\"modalimage\" alt=\"Expanded screenshot\">")
            .AppendLine("    </div>")
            .AppendLine("  </main>")
            .AppendLine("  <script>")
            .AppendLine(Js)
            .AppendLine("  </script>")
            .AppendLine("</body>")
            .AppendLine("</html>");

        File.WriteAllText(destination, html.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string GetDataImageString(ISearchContext context)
    {
        if (context is not ITakesScreenshot screenshotProvider) return string.Empty;

        var screenshot = screenshotProvider.GetScreenshot();
        var screenshotBytes = screenshot?.AsByteArray;

        return screenshotBytes is { Length: > 0 }
            ? $"data:image/png;base64,{Convert.ToBase64String(screenshotBytes)}"
            : string.Empty;
    }

    private static string GetCss(ISearchContext context)
    {
        var dataUrl = GetDataImageString(context);
        var escapedDataUrl = dataUrl.Replace("'", "%27", StringComparison.Ordinal);
        return Css.Replace("{{SCREENSHOT_URL}}", escapedDataUrl, StringComparison.Ordinal);
    }

    private static string GetContextContent(AxeResult results)
    {
        var testEnvironment = results.TestEnvironment;
        var testEngine = results.TestEngine;

        var context = new StringBuilder();
        context.Append("Url: ").Append(Encode(results.Url)).Append("<br>")
            .Append("Orientation: ").Append(Encode(testEnvironment?.OrientationType)).Append("<br>")
            .Append("Size: ").Append(Encode(testEnvironment?.WindowWidth)).Append(" x ")
                .Append(Encode(testEnvironment?.WindowHeight)).Append("<br>")
            .Append("Time: ").Append(Encode(results.Timestamp)).Append("<br>")
            .Append("User agent: ").Append(Encode(testEnvironment?.UserAgent)).Append("<br>")
            .Append("Using: ").Append(Encode(testEngine?.Name)).Append(" (")
                .Append(Encode(testEngine?.Version)).Append(')');

        return context.ToString();
    }

    private static string GetCountContent(
        int violationCount,
        int incompleteCount,
        int passCount,
        int inapplicableCount,
        AxeReportTypes requestedResults)
    {
        var countString = new StringBuilder();

        if (requestedResults.HasFlag(AxeReportTypes.Violations))
        {
            countString.Append("Violation: ").Append(violationCount).Append("<br>");
        }

        if (requestedResults.HasFlag(AxeReportTypes.Incomplete))
        {
            countString.Append("Incomplete: ").Append(incompleteCount).Append("<br>");
        }

        if (requestedResults.HasFlag(AxeReportTypes.Passes))
        {
            countString.Append("Pass: ").Append(passCount).Append("<br>");
        }

        if (requestedResults.HasFlag(AxeReportTypes.Inapplicable))
        {
            countString.Append("Inapplicable: ").Append(inapplicableCount);
        }

        return countString.ToString();
    }

    private static void AppendReadableAxeResults(StringBuilder html, IEnumerable<AxeResultItem> results, string type)
    {
        html.AppendLine("      <div class=\"resultWrapper\">")
            .AppendLine("        <button class=\"sectionbutton active\" type=\"button\">")
            .Append("          <h2 class=\"buttonInfoText\">")
                .Append(type)
                .Append(": ")
                .Append(GetCount(results))
                .AppendLine("</h2>")
            .AppendLine("          <h2 class=\"buttonExpandoText\">-</h2>")
            .AppendLine("        </button>")
            .Append("        <div class=\"majorSection\" id=\"")
                .Append(type)
                .AppendLine("Section\">");

        var loops = 1;
        foreach (var element in results)
        {
            html.Append("          <div class=\"findings\">")
                .Append(loops++)
                .Append(": ")
                .Append(Encode(element.Help))
                .AppendLine();

            html.Append("            <div class=\"emTwo\">")
                .Append("Description: ").Append(Encode(element.Description)).Append("<br>")
                .Append("Help: ").Append(Encode(element.Help)).Append("<br>")
                .Append("Help URL: <a href=\"").Append(EncodeAttribute(element.HelpUrl)).Append("\">")
                    .Append(Encode(element.HelpUrl)).Append("</a><br>");

            if (!string.IsNullOrEmpty(element.Impact))
            {
                html.Append("Impact: ").Append(Encode(element.Impact)).Append("<br>");
            }

            html.Append("Tags: ").Append(Encode(string.Join(", ", element.Tags ?? [])));

            if ((element.Nodes?.Length ?? 0) > 0)
            {
                html.Append("<br>Element(s):");
            }

            html.AppendLine("</div>");

            foreach (var item in element.Nodes ?? [])
            {
                html.AppendLine("            <div class=\"htmlTable\">")
                    .AppendLine("              <div class=\"emThree\">")
                    .AppendLine("                Html:")
                    .Append("                <p class=\"wrapOne\">")
                        .Append(Encode(item.Html))
                        .AppendLine("</p>")
                    .AppendLine("                Selector:")
                    .Append("                <p class=\"wrapTwo\">")
                        .Append(Encode(FormatTarget(item.Target)))
                        .AppendLine("</p>");

                AddFixes(html, item, type);

                html.AppendLine("              </div>")
                    .AppendLine("            </div>");
            }

            html.AppendLine("          </div>");
        }

        html.AppendLine("        </div>")
            .AppendLine("      </div>");
    }

    private static void AddFixes(StringBuilder html, AxeResultNode resultNode, string type)
    {
        var anyCheckResults = resultNode.Any ?? [];
        var allCheckResults = resultNode.All ?? [];
        var noneCheckResults = resultNode.None ?? [];

        var checkResultsCount = anyCheckResults.Length + allCheckResults.Length + noneCheckResults.Length;

        if (!string.Equals(type, "Violations", StringComparison.Ordinal) || checkResultsCount == 0) return;

        html.AppendLine("                To solve:")
            .AppendLine("                <p class=\"wrapTwo\"></p>");

        if (allCheckResults.Length > 0 || noneCheckResults.Length > 0)
        {
            FixAllIssues(html, allCheckResults, noneCheckResults);
        }

        if (anyCheckResults.Length > 0)
        {
            FixAnyIssues(html, anyCheckResults);
        }
    }

    private static void FixAllIssues(
        StringBuilder html,
        IReadOnlyCollection<AxeResultCheck> allCheckResults,
        IReadOnlyCollection<AxeResultCheck> noneCheckResults)
    {
        html.AppendLine("                <p class=\"wrapOne\">")
            .AppendLine("                  Fix all of the following issues:")
            .AppendLine("                  <ul>");

        foreach (var checkResult in allCheckResults.Concat(noneCheckResults))
        {
            html.Append("                    <li>")
                .Append(Encode((checkResult.Impact ?? string.Empty).ToUpperInvariant()))
                .Append(": ")
                .Append(Encode(checkResult.Message))
                .AppendLine("</li>");
        }

        html.AppendLine("                  </ul>")
            .AppendLine("                </p>");
    }

    private static void FixAnyIssues(StringBuilder html, IEnumerable<AxeResultCheck> anyCheckResults)
    {
        html.AppendLine("                <p class=\"wrapOne\">")
            .AppendLine("                  Fix at least one of the following issues:")
            .AppendLine("                  <ul>");

        foreach (var checkResult in anyCheckResults)
        {
            html.Append("                    <li>")
                .Append(Encode((checkResult.Impact ?? string.Empty).ToUpperInvariant()))
                .Append(": ")
                .Append(Encode(checkResult.Message))
                .AppendLine("</li>");
        }

        html.AppendLine("                  </ul>")
            .AppendLine("                </p>");
    }

    private static int GetCount(IEnumerable<AxeResultItem> results)
    {
        var count = 0;

        foreach (var item in results)
        {
            var nodeCount = item.Nodes?.Length ?? 0;
            count += nodeCount == 0 ? 1 : nodeCount;
        }

        return count;
    }

    private static string FormatTarget(object? target)
    {
        if (target is null) return string.Empty;
        if (target is string targetString) return targetString;

        if (target is IEnumerable targetEnumerable)
        {
            var targets = targetEnumerable
                .Cast<object?>()
                .Select(item => item?.ToString())
                .OfType<string>()
                .Where(item => !string.IsNullOrWhiteSpace(item));

            return string.Join('\n', targets);
        }

        return Convert.ToString(target, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Encode(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? string.Empty);

    private static string EncodeAttribute(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static readonly string Css = LoadEmbeddedResource("AxeHtmlReport.css");

    private static string LoadEmbeddedResource(string name)
    {
        using var stream = typeof(AxeHtmlReport).Assembly
            .GetManifestResourceStream($"Lombiq.Tests.UI.AccessibilityChecking.{name}");
        if (stream is null) throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static readonly string Js = LoadEmbeddedResource("AxeHtmlReport.js");
}
