using Lombiq.Tests.UI.Services;
using Microsoft.CodeAnalysis.Sarif;
using Shouldly;
using System;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanningConfiguration
{
    /// <summary>
    /// Gets or sets a delegate that may modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </summary>
    public Func<UITestContext, YamlDocument, Task> ZapAutomationFrameworkYamlModifier { get; set; }

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="SarifLog"/> when security scanning happens.
    /// </summary>
    public Action<UITestContext, SarifLog> AssertSecurityScanResult { get; set; } = AssertSecurityScanHasNoFails;

    public static readonly Action<UITestContext, SarifLog> AssertSecurityScanHasNoFails =
        (_, sarifLog) => sarifLog.Runs[0].Results.ShouldNotContain(result => result.Kind == ResultKind.Fail);

    // When running the app locally, HSTS is never set, so we'd get a "Strict-Transport-Security Header Not Set" fail.
    // The rule is disabled in the default configs though.
    public static readonly Action<UITestContext, SarifLog> AssertSecurityScanHasNoFailsExceptHsts =
        (_, sarifLog) =>
            sarifLog.Runs[0].Results.ShouldNotContain(result =>
                result.Kind == ResultKind.Fail && result.RuleId != "10035");
}
