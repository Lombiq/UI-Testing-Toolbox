#nullable enable

using Deque.AxeCore.Commons;
using OpenQA.Selenium;
using System;
using ServicesReportTypes = Lombiq.Tests.UI.AccessibilityChecking.AxeReportTypes;

namespace TWP.Selenium.Axe.Html;

/// <summary>
/// Backward-compatibility API shim for older consumers that import TWP.Selenium.Axe.Html.
/// </summary>
/// <remarks>
/// <para>
/// New code should use Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport directly.
/// </para>
/// </remarks>
[Obsolete("Use Lombiq.Tests.UI.AccessibilityChecking.AxeReportTypes instead.")]
[Flags]
public enum ReportTypes
{
    Violations = 1,
    Incomplete = 2,
    Inapplicable = 4,
    Passes = 8,
    All = Violations | Incomplete | Inapplicable | Passes,
}

/// <summary>
/// Backward-compatibility shim for the original HtmlReport extension class.
/// </summary>
[Obsolete("Use Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport instead.")]
public static class HtmlReport
{
    public static void CreateAxeHtmlReport(this IWebDriver webDriver, string destination) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(webDriver, destination);

    public static void CreateAxeHtmlReport(this IWebDriver webDriver, string destination, ReportTypes requestedResults) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(
            webDriver,
            destination,
            ToServicesReportTypes(requestedResults));

    public static void CreateAxeHtmlReport(this IWebDriver webDriver, IWebElement context, string destination) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(webDriver, context, destination);

    public static void CreateAxeHtmlReport(
        this IWebDriver webDriver,
        IWebElement context,
        string destination,
        ReportTypes requestedResults) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(
            webDriver,
            context,
            destination,
            ToServicesReportTypes(requestedResults));

    public static void CreateAxeHtmlReport(this ISearchContext context, AxeResult results, string destination) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(context, results, destination);

    public static void CreateAxeHtmlReport(
        this ISearchContext context,
        AxeResult results,
        string destination,
        ReportTypes requestedResults) =>
        Lombiq.Tests.UI.AccessibilityChecking.AxeHtmlReport.CreateAxeHtmlReport(
            context,
            results,
            destination,
            ToServicesReportTypes(requestedResults));

    private static ServicesReportTypes ToServicesReportTypes(ReportTypes reportTypes)
    {
        var mapped = 0;

        if (reportTypes.HasFlag(ReportTypes.Violations)) mapped |= (int)ServicesReportTypes.Violations;
        if (reportTypes.HasFlag(ReportTypes.Incomplete)) mapped |= (int)ServicesReportTypes.Incomplete;
        if (reportTypes.HasFlag(ReportTypes.Inapplicable)) mapped |= (int)ServicesReportTypes.Inapplicable;
        if (reportTypes.HasFlag(ReportTypes.Passes)) mapped |= (int)ServicesReportTypes.Passes;

        return (ServicesReportTypes)mapped;
    }
}
