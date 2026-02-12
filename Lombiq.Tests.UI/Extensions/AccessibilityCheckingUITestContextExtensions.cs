using Deque.AxeCore.Commons;
using Deque.AxeCore.Selenium;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using TWP.Selenium.Axe.Html;

namespace Lombiq.Tests.UI.Extensions;

public static class AccessibilityCheckingUITestContextExtensions
{
    /// <summary>
    /// Executes assertions on the result of an axe accessibility analysis. Note that you need to run this after every
    /// page load, it won't accumulate during a session.
    /// </summary>
    /// <param name="assertAxeResult">
    /// The assertion logic to run on the result of an axe accessibility analysis. If <see langword="null"/> then the
    /// assertion supplied in the context will be used.
    /// </param>
    /// <param name="axeBuilderConfigurator">
    /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator supplied
    /// in the context.
    /// </param>
    public static void AssertAccessibility(
        this UITestContext context,
        Action<AxeBuilder> axeBuilderConfigurator = null,
        Action<SimpleAxeResult> assertAxeResult = null)
    {
        var axeResult = context.AnalyzeAccessibility(axeBuilderConfigurator);
        var result = (SimpleAxeResult)axeResult;
        var accessibilityConfiguration = context.Configuration.AccessibilityCheckingConfiguration;

        try
        {
            if (accessibilityConfiguration.AxeResultIncompleteFilters.Count > 0 ||
                accessibilityConfiguration.AxeResultViolationsFilters.Count > 0)
            {
                result = FilterAccessibilityResults(result, accessibilityConfiguration);
            }

            (assertAxeResult ?? accessibilityConfiguration.AssertAxeResult)?.Invoke(result);
        }
        catch (Exception ex)
        {
            throw new AccessibilityAssertionException(
                axeResult,
                accessibilityConfiguration.CreateReportOnFailure,
                ex);
        }

        if (accessibilityConfiguration.CreateReportAlways)
        {
            var reportDirectoryPath = DirectoryHelper.CreateEnumeratedDirectory(
                context.GetTempSubDirectoryPath("AxeHtmlReport"));

            var reportPath = Path.Combine(
                    reportDirectoryPath,
                    context.TestManifest.Name.MakeFileSystemFriendly() + ".html");

            context.Driver.CreateAxeHtmlReport(axeResult, reportPath);

            context.AppendTestDump(reportPath);
        }
    }

    private static SimpleAxeResult FilterAccessibilityResults(
        SimpleAxeResult axeResult,
        AccessibilityCheckingConfiguration accessibilityConfiguration)
    {
        foreach (var filter in accessibilityConfiguration.AxeResultIncompleteFilters.Values)
        {
            axeResult.Incomplete.RemoveAll(item => item is null || !filter(item));
        }

        foreach (var filter in accessibilityConfiguration.AxeResultViolationsFilters.Values)
        {
            axeResult.Violations.RemoveAll(item => item is null || !filter(item));
        }

        return axeResult;
    }

    /// <summary>
    /// Runs an axe accessibility analysis. Note that you need to run this after every page load, it won't accumulate
    /// during a session.
    /// </summary>
    /// <param name="axeBuilderConfigurator">
    /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator supplied
    /// in the context.
    /// </param>
    public static AxeResult AnalyzeAccessibility(
        this UITestContext context,
        Action<AxeBuilder> axeBuilderConfigurator = null)
    {
        var axeBuilder = new AxeBuilder(context.Scope.Driver);
        context.Configuration.AccessibilityCheckingConfiguration.AxeBuilderConfigurator?.Invoke(axeBuilder);
        axeBuilderConfigurator?.Invoke(axeBuilder);
        return axeBuilder.Analyze();
    }
}
