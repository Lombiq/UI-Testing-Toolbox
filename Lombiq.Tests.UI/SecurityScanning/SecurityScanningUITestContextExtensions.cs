using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class SecurityScanningUITestContextExtensions
{
    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app with the Baseline
    /// Automation Framework profile (see <see href="https://www.zaproxy.org/docs/docker/baseline-scan/"/> for the
    /// official docs on the legacy version of this scan).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public static Task RunBaselineSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<object, Task> modifyYaml = null) =>
        context.RunSecurityScanAsync(AutomationFrameworkYamlPaths.BaselineYamlPath, startUri, modifyYaml);

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app with the Full Scan
    /// Automation Framework profile (see <see href="https://www.zaproxy.org/docs/docker/full-scan/"/> for the
    /// official docs on the legacy version of this scan).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public static Task RunFullSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<object, Task> modifyYaml = null) =>
        context.RunSecurityScanAsync(AutomationFrameworkYamlPaths.FullScanYamlPath, startUri, modifyYaml);

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app with the GraphQL
    /// Automation Framework profile (see <see href="https://www.zaproxy.org/docs/desktop/addons/graphql-support/"/> for
    /// the official docs on ZAP's GraphQL support).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public static Task RunGraphQLSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<object, Task> modifyYaml = null) =>
        context.RunSecurityScanAsync(AutomationFrameworkYamlPaths.GraphQLYamlPath, startUri, modifyYaml);

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app with the OpenAPI
    /// Automation Framework profile (see <see href="https://www.zaproxy.org/docs/desktop/addons/openapi-support/"/> for
    /// the official docs on ZAP's GraphQL support).
    /// </summary>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public static Task RunOpenApiSecurityScanAsync(
        this UITestContext context,
        Uri startUri = null,
        Func<object, Task> modifyYaml = null) =>
        context.RunSecurityScanAsync(AutomationFrameworkYamlPaths.BaselineYamlPath, startUri, modifyYaml);

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app.
    /// </summary>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See
    /// <see href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="startUri">
    /// The <see cref="Uri"/> under the app where to start the scan from. If not provided, defaults to the current URL.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public static Task RunSecurityScanAsync(
        this UITestContext context,
        string automationFrameworkYamlPath,
        Uri startUri = null,
        Func<object, Task> modifyYaml = null) =>
        context.ZapManager.RunSecurityScanAsync(context, automationFrameworkYamlPath, startUri ?? context.GetCurrentUri(), modifyYaml);
}
