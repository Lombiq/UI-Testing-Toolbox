using Lombiq.Tests.UI.Services;
using Microsoft.CodeAnalysis.Sarif;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanningConfiguration
{
    /// <summary>
    /// Gets or sets a delegate that may modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </summary>
    public Func<UITestContext, object, Task> ZapAutomationFrameworkYamlModifier { get; set; }

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="SarifLog"/> when security scanning happens.
    /// </summary>
    public Action<UITestContext, SarifLog> AssertSecurityScanResult { get; set; } = AssertSecurityScanHasNoFails;

    public static readonly Action<UITestContext, SarifLog> AssertSecurityScanHasNoFails =
        (_, sarifLog) => sarifLog.Runs[0].Results.ShouldNotContain(result => result.Kind == ResultKind.Fail);
}
