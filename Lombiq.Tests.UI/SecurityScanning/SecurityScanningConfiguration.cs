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
    /// Gets or sets a delegate that may modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </summary>
    public Func<UITestContext, YamlDocument, Task> ZapAutomationFrameworkYamlModifier { get; set; }

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="SarifLog"/> when security scanning happens.
    /// </summary>
    public Action<UITestContext, SarifLog> AssertSecurityScanResult { get; set; } = AssertSecurityScanHasNoAlerts;

    public static readonly Action<UITestContext, SarifLog> AssertSecurityScanHasNoAlerts =
        (_, sarifLog) => sarifLog.Runs[0].Results.ShouldNotContain(result =>
            result.Kind == ResultKind.Fail && result.Level != FailureLevel.None && result.Level != FailureLevel.Note);
}
