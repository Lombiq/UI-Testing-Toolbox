using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.GitHub;
using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

/// <summary>
/// Service to manage <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> instances and security scans
/// for a given test.
/// </summary>
public sealed class ZapManager : IAsyncDisposable
{
    // Using the then-latest stable release of ZAP. You can check for newer version tags here:
    // https://hub.docker.com/r/softwaresecurityproject/zap-stable/tags.
    // When updating this version, also regenerate the Automation Framework YAML config files so we don't miss any
    // changes to those.
    private const string _zapImage = "softwaresecurityproject/zap-stable:2.14.0"; // #spell-check-ignore-line
    private const string _zapWorkingDirectoryPath = "/zap/wrk/"; // #spell-check-ignore-line
    private const string _zapReportsDirectoryName = "reports";

    private static readonly SemaphoreSlim _pullSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");
    private static readonly PortLeaseManager _portLeaseManager;

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private static bool _wasPulled;

    private int _zapPort;

    static ZapManager()
    {
        var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
        _portLeaseManager = new PortLeaseManager(15000 + agentIndexTimesHundred, 15099 + agentIndexTimesHundred);
    }

    internal ZapManager(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Run a <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> security scan against an app.
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

        // Each attempt will have it's own "ZapN" directory inside the temp, starting with "Zap1".
        var mountedDirectoryPath = DirectoryHelper.CreateEnumeratedDirectory(
            DirectoryPaths.GetTempSubDirectoryPath(context.Id, "Zap"));
        var reportsDirectoryPath = Path.Combine(mountedDirectoryPath, _zapReportsDirectoryName);

        Directory.CreateDirectory(reportsDirectoryPath);

        // Giving write permission to all users to the reports folder. This is to avoid issues under GitHub-hosted
        // runners in GitHub Actions (BuildJet ones work without this too) at ZAP not being able to create the report.
        // Pre-creating the report's folder would just prompt ZAP to try another folder name suffixed with "2".
        if (GitHubHelper.IsGitHubEnvironment)
        {
            await new CliProgram("chmod").ExecuteAsync(_cancellationTokenSource.Token, "a+w", reportsDirectoryPath);
        }

        var yamlFileName = Path.GetFileName(automationFrameworkYamlPath);
        var yamlFileCopyPath = Path.Combine(mountedDirectoryPath, yamlFileName);

        File.Copy(automationFrameworkYamlPath, yamlFileCopyPath, overwrite: true);

        await PreparePlanAsync(yamlFileCopyPath, modifyPlan);

        // Explanation on the CLI arguments used below:
        // - --add-host and --network host: Lets us connect to the host OS's localhost, where the OC app runs, with
        //   https://localhost. See https://stackoverflow.com/a/24326540/220230. --network host serves the same, but
        //   only works under Linux. See https://docs.docker.com/engine/reference/commandline/run/#network and
        //   https://docs.docker.com/network/drivers/host/.
        // - --rm: Removes the container after completion. Otherwise, unused containers would pile up in Docker. See
        //   https://docs.docker.com/engine/reference/run/#clean-up---rm for the official docs.
        // - --volume: Mounts the given host folder as a volume under the given container path. This is to pass files
        //   back and forth between the host and the container.
        // - --tty: Allocates a pseudo-teletypewriter, i.e. redirects the output of ZAP to the CLI's output.
        // - zap.sh: The entry point of ZAP. Everything that comes after this is executed in the container.

        // Also see https://www.zaproxy.org/docs/docker/about/#automation-framework.

        // Running a ZAP desktop in the browser with Webswing with the same config under Windows: #spell-check-ignore-line
#pragma warning disable S103 // Lines should not be too long
        // docker run --add-host localhost:host-gateway -u zap -p 8080:8080 -p 8090:8090 -i softwaresecurityproject/zap-stable zap-webswing.sh  #spell-check-ignore-line
#pragma warning restore S103 // Lines should not be too long

        var cliParameters = new List<object> { "run" };

        if (OperatingSystem.IsLinux())
        {
            cliParameters.Add("--network");
            cliParameters.Add("host");
        }
        else
        {
            cliParameters.Add("--add-host");
            cliParameters.Add("localhost:host-gateway");
        }

        // Using a different port than the default 8080 is necessary so ZAP doesn't clash with other web processes and
        // to allow more than one security scan to run at the same time.
        _zapPort = await _portLeaseManager.LeaseAvailableRandomPortAsync();
        _testOutputHelper.WriteLineTimestampedAndDebug("Running ZAP on port {0}.", _zapPort);

        cliParameters.AddRange(new object[]
        {
            "--rm",
            "--volume",
            $"{mountedDirectoryPath}:{_zapWorkingDirectoryPath}:rw",
            "--tty",
            _zapImage,
            "zap.sh",
            "-cmd",
            "-autorun",
            _zapWorkingDirectoryPath + yamlFileName,
            "-port",
            _zapPort,
        });

        var stdErrBuffer = new StringBuilder();

        // Here we use a new container instance for every scan. This is viable and is not an overhead big enough to
        // worry about, but an optimization would be to run multiple scans (also possible simultaneously with background
        // commands) with the same instance. This needs the -dir option to configure a different home directory per
        // scan, see https://www.zaproxy.org/docs/desktop/cmdline/#options.
        var result = await _docker
            .GetCommand(cliParameters)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _testOutputHelper.WriteLineTimestampedAndDebug(line)))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            // This is so no exception is thrown by CliWrap if the exit code is not 0.
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(_cancellationTokenSource.Token);

        _testOutputHelper.WriteLineTimestampedAndDebug("Security scanning completed with the exit code {0}.", result.ExitCode);

        if (result.ExitCode == 1)
        {
            throw new SecurityScanningException("Security scanning didn't successfully finish. Check the test's output log for details.");
        }

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

    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }

        await _portLeaseManager.StopLeaseAsync(_zapPort);
    }

    private async Task EnsureInitializedAsync()
    {
        try
        {
            var token = _cancellationTokenSource.Token;

            await _pullSemaphore.WaitAsync(token);

            if (_wasPulled) return;

            // Without --quiet, "What's Next?" hints will be written to stderr by Docker. See
            // https://github.com/docker/for-mac/issues/6904.
            await _docker.ExecuteAsync(token, "pull", _zapImage, "--quiet");
            _wasPulled = true;
        }
        finally
        {
            _pullSemaphore.Release();
        }
    }

    private static async Task PreparePlanAsync(string yamlFilePath, Func<YamlDocument, Task> modifyPlan)
    {
        var yamlDocument = YamlHelper.LoadDocument(yamlFilePath);

        // Setting report directories to the conventional one and verifying that there's exactly one SARIF report.

        var sarifReportCount = 0;

        foreach (var job in yamlDocument.GetJobs())
        {
            if ((string)job["type"] != "report") continue;

            var parameters = (YamlMappingNode)job["parameters"];
            parameters["reportDir"].SetValue(_zapWorkingDirectoryPath + _zapReportsDirectoryName);

            if ((string)parameters["template"] == "sarif-json") sarifReportCount++;
        }

        if (sarifReportCount != 1)
        {
            throw new ArgumentException(
                "The supplied ZAP Automation Framework YAML file should contain exactly one SARIF report job.");
        }

        if (modifyPlan != null) await modifyPlan(yamlDocument);

        using var streamWriter = new StreamWriter(yamlFilePath);
        var yamlStream = new YamlStream(yamlDocument);
        yamlStream.Save(streamWriter, assignAnchors: false);
    }
}
