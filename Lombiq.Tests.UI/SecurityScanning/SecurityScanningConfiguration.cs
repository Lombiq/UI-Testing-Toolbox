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
    /// Gets or sets a value indicating whether to save a report to the test dump for every scan, even passing ones.
    /// </summary>
    public bool CreateReportAlways { get; set; }

    /// <summary>
    /// Gets or sets a delegate that may modify the deserialized representation of the ZAP Automation Framework plan in
    /// YAML.
    /// </summary>
    public Func<UITestContext, YamlDocument, Task> ZapAutomationFrameworkPlanModifier { get; set; }

    /// <summary>
    /// Gets or sets the log level for the ZAP security scanning. Defaults to <see cref="ZapLogLevel.Info"/>. These log
    /// levels correspond to <see href="https://logging.apache.org/log4j/2.x/manual/customloglevels.html">Log4j's
    /// standard log levels</see>. Also see <see
    /// href="https://www.zaproxy.org/faq/how-do-you-configure-zap-logging/">the docs on ZAP's logging</see>. Note that
    /// using a log level more granular than <see cref="ZapLogLevel.Info"/> (like <see cref="ZapLogLevel.Debug"/> slows
    /// down the security scan considerably (can even double the runtime), so use it only when necessary.
    /// </summary>
    public ZapLogLevel ZapLogLevel { get; set; } = ZapLogLevel.Info;

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="SarifLog"/> when security scanning happens.
    /// </summary>
    public Action<UITestContext, SarifLog> AssertSecurityScanResult { get; set; } = AssertSecurityScanHasNoAlerts;

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

public enum ZapLogLevel
{
    Off,
    Fatal,
    Error,
    Warn,
    Info,
    Debug,
    Trace,
    All,
}
