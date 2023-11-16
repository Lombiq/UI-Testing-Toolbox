using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

/// <summary>
/// Service to manage Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) instances and security scans.
/// </summary>
public sealed class ZapManager : IAsyncDisposable
{
    // Need to use the weekly release because that's the one that has packaged scans migrated to Automation Framework.
    // When updating this version, also regenerate the Automation Framework YAML config files so we don't miss any
    // changes to those.
    private const string _zapImage = "softwaresecurityproject/zap-weekly:20231113";
    private const string _zapWorkingDirectoryPath = "/zap/wrk/";
    private const string _zapReportsDirectoryName = "reports";

    private static readonly SemaphoreSlim _pullSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");

    private readonly ITestOutputHelper _testOutputHelper;

    private static bool _wasPulled;

    private CancellationTokenSource _cancellationTokenSource;

    internal ZapManager(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> of the currently executing test.</param>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See
    /// <see href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="startUri">The <see cref="Uri"/> under the app where to start the scan from.</param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <returns>
    /// A <see cref="SecurityScanResult"/> instance containing the SARIF (<see
    /// href="https://sarifweb.azurewebsites.net/"/>) report of the scan.
    /// </returns>
    public Task<SecurityScanResult> RunSecurityScanAsync(
        UITestContext context,
        string automationFrameworkYamlPath,
        Uri startUri,
        Func<YamlDocument, Task> modifyPlan = null) =>
        RunSecurityScanAsync(
            context,
            automationFrameworkYamlPath,
            async configuration =>
            {
                SetStartUrlInYaml(configuration, startUri);
                if (modifyPlan != null) await modifyPlan(configuration);
            });

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> of the currently executing test.</param>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See <see
    /// href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the ZAP Automation Framework plan in YAML.
    /// </param>
    /// <returns>
    /// A <see cref="SecurityScanResult"/> instance containing the SARIF (<see
    /// href="https://sarifweb.azurewebsites.net/"/>) report of the scan.
    /// </returns>
    public async Task<SecurityScanResult> RunSecurityScanAsync(
        UITestContext context,
        string automationFrameworkYamlPath,
        Func<YamlDocument, Task> modifyPlan = null)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrEmpty(automationFrameworkYamlPath))
        {
            automationFrameworkYamlPath = AutomationFrameworkPlanPaths.BaselinePlanPath;
        }

        var mountedDirectoryPath = DirectoryPaths.GetTempSubDirectoryPath(context.Id, "Zap");
        Directory.CreateDirectory(mountedDirectoryPath);

        var yamlFileName = Path.GetFileName(automationFrameworkYamlPath);
        var yamlFileCopyPath = Path.Combine(mountedDirectoryPath, yamlFileName);

        File.Copy(automationFrameworkYamlPath, yamlFileCopyPath, overwrite: true);

        await PrepareYamlAsync(yamlFileCopyPath, modifyPlan);

        // Explanation on the CLI arguments used below:
        // - --add-host and --network host: Lets us connect to the host OS's localhost, where the OC app runs, with
        //   https://localhost. See https://stackoverflow.com/a/24326540/220230. --network host serves the same, but
        //   only works under Linux. See https://docs.docker.com/engine/reference/commandline/run/#network and
        //   https://docs.docker.com/network/drivers/host/.
        // - --volume: Mounts the given host folder as a volume under the given container path. This is to pass files
        //   back and forth between the host and the container.
        // - --tty: Allocates a pseudo-teletypewriter, i.e. redirects the output of ZAP to the CLI's output.
        // - zap.sh: The entry point of ZAP. Everything that comes after this is executed in the container.

        // Also see https://www.zaproxy.org/docs/docker/about/#automation-framework.

        // Running a ZAP desktop in the browser with Webswing with the same config under Windows:
#pragma warning disable S103 // Lines should not be too long
        // docker run --add-host localhost:host-gateway -u zap -p 8080:8080 -p 8090:8090 -i softwaresecurityproject/zap-weekly:20231113 zap-webswing.sh
#pragma warning restore S103 // Lines should not be too long

        var cliParameters = new List<object> { "run" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            cliParameters.Add("--network");
            cliParameters.Add("host");
        }
        else
        {
            cliParameters.Add("--add-host");
            cliParameters.Add("localhost:host-gateway");
        }

        cliParameters.AddRange(new object[]
        {
            "--volume",
            $"{mountedDirectoryPath}:{_zapWorkingDirectoryPath}:rw",
            "--tty",
            _zapImage,
            "zap.sh",
            "-cmd",
            "-autorun",
            _zapWorkingDirectoryPath + yamlFileName,
        });

        var stdErrBuffer = new StringBuilder();

        // The result of the call is not interesting, since we don't need the exit code: Assertions should check if the
        // app failed security scanning, and if the scan itself fails then there won't be a report, what's checked below.
        await _docker
            .GetCommand(cliParameters)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _testOutputHelper.WriteLineTimestampedAndDebug(line)))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            // This is so no exception is thrown by CliWrap if the exit code is not 0.
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(_cancellationTokenSource.Token);

        var reportsDirectoryPath = Path.Combine(mountedDirectoryPath, _zapReportsDirectoryName);

        var jsonReports = Directory.EnumerateFiles(reportsDirectoryPath, "*.json").ToList();

        if (jsonReports.Count > 1)
        {
            throw new SecurityScanningException(
                "There were more than one JSON reports generated for the ZAP scan. The supplied ZAP Automation " +
                "Framework YAML file should contain exactly one JSON report job, generating a SARIF report.");
        }

        if (jsonReports.Count != 1)
        {
            throw new SecurityScanningException(
                "No SARIF JSON report was generated for the ZAP scan. This indicates that the scan couldn't finish. " +
                "Check the test output for details.");
        }

        return new SecurityScanResult(reportsDirectoryPath, SarifLog.Load(jsonReports[0]));
    }

    public ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_cancellationTokenSource != null) return;

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            await _pullSemaphore.WaitAsync(token);

            if (!_wasPulled)
            {
                await _docker.ExecuteAsync(token, "pull", _zapImage);
                _wasPulled = true;
            }
        }
        finally
        {
            _pullSemaphore.Release();
        }
    }

    private static void SetStartUrlInYaml(YamlDocument configuration, Uri startUri)
    {
        var rootNode = (YamlMappingNode)configuration.RootNode;

        var contexts = (YamlSequenceNode)rootNode["env"]["contexts"];

        if (!contexts.Any())
        {
            throw new ArgumentException(
                "The supplied ZAP Automation Framework YAML file should contain at least one context.");
        }

        var currentContext = (YamlMappingNode)contexts[0];

        if (contexts.Count() > 1)
        {
            currentContext = (YamlMappingNode)contexts.FirstOrDefault(context => context["Name"].ToString() == "Default Context")
                ?? currentContext;
        }

        // Setting URLs in the context.
        // Setting includePaths in the context is not necessary because by default everything under urls will be scanned.

        if (!currentContext.Children.ContainsKey("urls")) currentContext.Add("urls", new YamlSequenceNode());

        var urls = (YamlSequenceNode)currentContext["urls"];
        var urlsCount = urls.Count();

        if (urlsCount > 1)
        {
            throw new ArgumentException(
                "The context in the ZAP Automation Framework YAML file should contain at most a single url in the urls section.");
        }

        if (urlsCount == 1) urls.Children.Clear();

        urls.Add(startUri.ToString());
    }

    private static async Task PrepareYamlAsync(string yamlFilePath, Func<YamlDocument, Task> modifyPlan)
    {
        YamlDocument yamlDocument;

        using (var streamReader = new StreamReader(yamlFilePath))
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(streamReader);
            // Using a free-form object instead of deserializing into a statically defined object, not to potentially
            // break unknown fields during reserialization.
            yamlDocument = yamlStream.Documents[0];
        }

        var rootNode = (YamlMappingNode)yamlDocument.RootNode;

        // Setting report directories to the conventional one and verifying that there's exactly one SARIF report.

        var jobs = (IEnumerable<YamlNode>)rootNode["jobs"];

        var sarifReportCount = 0;

        foreach (var job in jobs)
        {
            if ((string)job["type"] != "report") continue;

            var parameters = (YamlMappingNode)job["parameters"];
            ((YamlScalarNode)parameters["reportDir"]).Value = _zapWorkingDirectoryPath + _zapReportsDirectoryName;

            if ((string)parameters["template"] == "sarif-json") sarifReportCount++;
        }

        if (sarifReportCount != 1)
        {
            throw new ArgumentException(
                "The supplied ZAP Automation Framework YAML file should contain exactly one SARIF report job.");
        }

        if (modifyPlan != null) await modifyPlan(yamlDocument);

        using (var streamWriter = new StreamWriter(yamlFilePath))
        {
            var yamlStream = new YamlStream(yamlDocument);
            yamlStream.Save(streamWriter, assignAnchors: false);
        }
    }
}
