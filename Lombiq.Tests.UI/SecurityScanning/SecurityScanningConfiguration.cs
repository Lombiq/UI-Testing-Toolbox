using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Microsoft.CodeAnalysis.Sarif;
using Shouldly;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanningConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to save a report to the failure dump for every scan, even passing ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Won't work until https://github.com/Lombiq/UI-Testing-Toolbox/issues/323 is implemented, hence it's internal.
    /// </para>
    /// </remarks>
    internal bool CreateReportAlways { get; set; }

    /// <summary>
    /// Gets or sets a delegate that may modify the deserialized representation of the ZAP Automation Framework plan in
    /// YAML.
    /// </summary>
    public Func<UITestContext, YamlDocument, Task> ZapAutomationFrameworkPlanModifier { get; set; }

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="SarifLog"/> when security scanning happens.
    /// </summary>
    public Action<UITestContext, SarifLog> AssertSecurityScanResult { get; set; } = AssertSecurityScanHasNoAlerts;

    /// <summary>
    /// Gets a value indicating whether the security scan should not visit the <see cref="ErrorController"/> to test
    /// for correct error handling. This is achieved by running the scan a second time without leaving that page.
    /// </summary>
    public bool DontScanErrorPage { get; private set; }

    public static readonly Action<UITestContext, SarifLog> AssertSecurityScanHasNoAlerts = (_, sarifLog) =>
        sarifLog.Runs[0].Results.ShouldBeEmptyWhen(
            result =>
                result.Kind == ResultKind.Fail &&
                result.Level != FailureLevel.None &&
                result.Level != FailureLevel.Note,
            result => new
            {
                Kind = result.Kind.ToString(),
                Level = result.Level.ToString(),
                Details = result,
            });
}
