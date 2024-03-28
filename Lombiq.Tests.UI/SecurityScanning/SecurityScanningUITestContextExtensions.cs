using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class SecurityScanningUITestContextExtensions
{
    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// Baseline Automation Framework profile except for the spiderAjax job, and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/docker/baseline-scan/"/> for the official docs on the legacy version of this
    /// scan).
    /// </summary>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertBaselineSecurityScanAsync(
        this UITestContext context,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.BaselinePlanPath,
            configure,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// Full Scan Automation Framework profile except for the spiderAjax job, and runs assertions on the result (see
    /// <see href="https://www.zaproxy.org/docs/docker/full-scan/"/> for the official docs on the legacy version of this
    /// scan).
    /// </summary>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertFullSecurityScanAsync(
        this UITestContext context,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.FullScanPlanPath,
            configure,
            assertSecurityScanResult);

    /// <inheritdoc cref="RunAndAssertFullSecurityScanAsync"/>
    /// <param name="doSignIn">If <see langword="true"/> the bot is configured to sign in as <c>admin</c> first.</param>
    /// <param name="maxActiveScanDurationInMinutes">Time limit for the active scan altogether.</param>
    /// <param name="maxRuleDurationInMinutes">Time limit for the individual rules in the active scan.</param>
    /// <remarks><para>
    /// This extension method makes changes to the normal configuration of the test to be more suited for CI operation.
    /// It changes the <see cref="UITestContext.Configuration"/> to not do any retries because this is a long running
    /// test. It also replaces the app log assertion logic with the specialized version for security scans, <see
    /// cref="OrchardCoreUITestExecutorConfiguration.UseAssertAppLogsForSecurityScan"/>. The scan is configured t
    /// ignore the admin dashboard, optionally log in as admin, and use the provided time limits for the "active scan"
    /// portion of the security scan.
    /// </para></remarks>
    public static Task RunAndConfigureAndAssertFullSecurityScanForContinuousIntegrationAsync(
        this UITestContext context,
        Action<SecurityScanConfiguration> additionalConfiguration = null,
        Action<SarifLog> assertSecurityScanResult = null,
        bool doSignIn = true,
        int maxActiveScanDurationInMinutes = 10,
        int maxRuleDurationInMinutes = 2)
    {
        // Ignore some validation errors that only happen during security tests.
        context.Configuration.UseAssertAppLogsForSecurityScan();

        // This can take even over 10 minutes and the CI session would certainly time out with retries.
        context.Configuration.MaxRetryCount = 0;

        return context.RunAndAssertFullSecurityScanAsync(
            configuration =>
            {
                // Signing in ensures full access and that the bot won't have to interact with the login screen.
                if (doSignIn) configuration.SignIn();

                // There is no need to security scan the admin dashboard.
                configuration.ExcludeUrlWithRegex(@".*/Admin/.*");

                // Active scan takes a very long time, this is not practical in CI.
                configuration.ModifyZapPlan(plan => plan
                    .SetActiveScanMaxDuration(maxActiveScanDurationInMinutes, maxRuleDurationInMinutes));

                additionalConfiguration?.Invoke(configuration);
            },
            assertSecurityScanResult);
    }

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// GraphQL Automation Framework profile and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/desktop/addons/graphql-support/"/> for the official docs on ZAP's GraphQL
    /// support).
    /// </summary>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertGraphQLSecurityScanAsync(
        this UITestContext context,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.GraphQLPlanPath,
            configure,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// OpenAPI Automation Framework profile and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/desktop/addons/openapi-support/"/> for the official docs on ZAP's GraphQL
    /// support).
    /// </summary>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertOpenApiSecurityScanAsync(
        this UITestContext context,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.OpenAPIPlanPath,
            configure,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app and runs
    /// assertions on the result.
    /// </summary>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See <see
    /// href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    /// <returns>
    /// A <see cref="SecurityScanResult"/> instance containing the SARIF (<see
    /// href="https://sarifweb.azurewebsites.net/"/>) report of the scan.
    /// </returns>
    public static async Task RunAndAssertSecurityScanAsync(
        this UITestContext context,
        string automationFrameworkYamlPath,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null)
    {
        var configuration = context.Configuration.SecurityScanningConfiguration ?? new SecurityScanningConfiguration();

        SecurityScanResult result = null;
        try
        {
            result = await context.RunSecurityScanAsync(automationFrameworkYamlPath, scanConfiguration =>
            {
                // Verify that error page handling also works by visiting a known error page with no logging.
                if (!scanConfiguration.DontScanErrorPage)
                {
                    var errorUrl = context.GetAbsoluteUrlOfAction<ErrorController>(controller => controller.Index());
                    scanConfiguration.ModifyZapPlan(yamlDocument => yamlDocument.AddRequestor(errorUrl.AbsoluteUri));
                }

                configure?.Invoke(scanConfiguration);
            });

            if (assertSecurityScanResult != null) assertSecurityScanResult(result.SarifLog);
            else configuration.AssertSecurityScanResult(context, result.SarifLog);

            if (configuration.CreateReportAlways)
            {
                context.AppendDirectoryToFailureDump(result.ReportsDirectoryPath);
            }
        }
        catch (Exception ex)
        {
            if (result != null) context.AppendDirectoryToFailureDump(result.ReportsDirectoryPath);
            throw new SecurityScanningAssertionException(ex);
        }
    }

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app.
    /// </summary>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See
    /// <see href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="configure">A delegate to configure the security scan in detail.</param>
    /// <returns>
    /// A <see cref="SecurityScanResult"/> instance containing the SARIF (<see
    /// href="https://sarifweb.azurewebsites.net/"/>) report of the scan.
    /// </returns>
    public static Task<SecurityScanResult> RunSecurityScanAsync(
        this UITestContext context,
        string automationFrameworkYamlPath,
        Action<SecurityScanConfiguration> configure = null)
    {
        var configuration = new SecurityScanConfiguration()
            .StartAtUri(context.GetCurrentUri());

        // By default ignore /vendor/ or /vendors/ URLs. This is case-insensitive. We have no control over them, and
        // they may contain several false positives (e.g. in font-awesome).
        configuration.ExcludeUrlWithRegex(@".*/vendors?/.*");

        if (context.Configuration.SecurityScanningConfiguration.ZapAutomationFrameworkPlanModifier != null)
        {
            configuration.ModifyZapPlan(async plan =>
                await context.Configuration.SecurityScanningConfiguration.ZapAutomationFrameworkPlanModifier(context, plan));
        }

        configure?.Invoke(configuration);

        return context.ZapManager.RunSecurityScanAsync(
            context,
            automationFrameworkYamlPath,
            async plan => await configuration.ApplyToPlanAsync(plan, context));
    }
}
