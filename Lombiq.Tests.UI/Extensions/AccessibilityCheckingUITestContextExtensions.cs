using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using System;
using System.IO;

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
        Action<AxeResult> assertAxeResult = null)
    {
        var axeResult = context.AnalyzeAccessibility(axeBuilderConfigurator);
        var accessibilityConfiguration = context.Configuration.AccessibilityCheckingConfiguration;

        try
        {
            (assertAxeResult ?? accessibilityConfiguration.AssertAxeResult)?.Invoke(axeResult);
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
            context.Driver.CreateAxeHtmlReport(
                axeResult,
                Path.Combine(
                    accessibilityConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath,
                    context.TestManifest.Name.MakeFileSystemFriendly() + ".html"));
        }
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
