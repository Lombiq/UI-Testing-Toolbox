using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class SecurityScanningUITestContextExtensions
{
    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// Baseline Automation Framework profile except for the spiderAjax job, and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/docker/baseline-scan/"/> for the official docs on the legacy version of this
    /// scan).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertBaselineSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.BaselinePlanPath,
            startUri,
            modifyPlan,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// Full Scan Automation Framework profile except for the spiderAjax job, and runs assertions on the result (see
    /// <see href="https://www.zaproxy.org/docs/docker/full-scan/"/> for the official docs on the legacy version of this
    /// scan).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertFullSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.FullScanPlanPath,
            startUri,
            modifyPlan,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// GraphQL Automation Framework profile and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/desktop/addons/graphql-support/"/> for the official docs on ZAP's GraphQL
    /// support).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertGraphQLSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.GraphQLPlanPath,
            startUri,
            modifyPlan,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app with the
    /// OpenAPI Automation Framework profile and runs assertions on the result (see <see
    /// href="https://www.zaproxy.org/docs/desktop/addons/openapi-support/"/> for the official docs on ZAP's GraphQL
    /// support).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <param name="assertSecurityScanResult">
    /// A delegate to run assertions on the <see cref="SarifLog"/> one the scan finishes.
    /// </param>
    public static Task RunAndAssertOpenApiSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null,
        Action<SarifLog> assertSecurityScanResult = null) =>
        context.RunAndAssertSecurityScanAsync(
            AutomationFrameworkPlanPaths.OpenAPIPlanPath,
            startUri,
            modifyPlan,
            assertSecurityScanResult);

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app and runs
    /// assertions on the result.
    /// </summary>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See <see
    /// href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
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
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null,
        Action<SarifLog> assertSecurityScanResult = null)
    {
        var configuration = context.Configuration.SecurityScanningConfiguration;

        async Task CompositemodifyPlan(YamlDocument plan)
        {
            if (configuration.ZapAutomationFrameworkYamlModifier != null)
            {
                await configuration.ZapAutomationFrameworkYamlModifier(context, plan);
            }

            if (modifyPlan != null) await modifyPlan(plan);
        }

        SecurityScanResult result = null;
        try
        {
            result = await context.RunSecurityScanAsync(automationFrameworkYamlPath, startUri, CompositemodifyPlan);

            if (assertSecurityScanResult != null) assertSecurityScanResult(result.SarifLog);
            else configuration.AssertSecurityScanResult(context, result.SarifLog);

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
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <returns>
    /// A <see cref="SecurityScanResult"/> instance containing the SARIF (<see
    /// href="https://sarifweb.azurewebsites.net/"/>) report of the scan.
    /// </returns>
    public static Task<SecurityScanResult> RunSecurityScanAsync(
        this UITestContext context,
        string automationFrameworkYamlPath,
        Uri startUri = null,
        Func<YamlDocument, Task> modifyPlan = null) =>
        context.ZapManager.RunSecurityScanAsync(context, automationFrameworkYamlPath, startUri ?? context.GetCurrentUri(), modifyPlan);
}
