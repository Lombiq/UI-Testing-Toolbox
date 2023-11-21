using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
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
        Uri startUri = null,
        Action<SecurityScanConfiguration> configure = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.FullScanPlanPath,
            configure,
            assertSecurityScanResult);

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
        Uri startUri = null,
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
        Uri startUri = null,
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
        var configuration = context.Configuration.SecurityScanningConfiguration;

        SecurityScanResult result = null;
        try
        {
            result = await context.RunSecurityScanAsync(automationFrameworkYamlPath, configure);

            if (assertSecurityScanResult != null) assertSecurityScanResult(result.SarifLog);
            else configuration?.AssertSecurityScanResult(context, result.SarifLog);

            if (configuration.CreateReportAlways) context.AppendDirectoryToFailureDump(result.ReportsDirectoryPath);
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
        var configuration = new SecurityScanConfiguration();

        configuration.StartAtUri(context.GetCurrentUri());

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
