using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.SecurityScanning;

public sealed class ZapManager : IAsyncDisposable
{
    // Need to use the weekly release because that's the one that has packaged scans migrated to Automation Framework.
    private const string _zapImage = "softwaresecurityproject/zap-weekly:20231113";

    private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");

    private readonly ITestOutputHelper _testOutputHelper;

    private static bool _wasPulled;

    private CancellationTokenSource _cancellationTokenSource;

    internal ZapManager(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    public async Task RunSecurityScanAsync(
        UITestContext context,
        Uri startUri,
        string automationFrameworkYamlPath = null)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrEmpty(automationFrameworkYamlPath))
        {
            automationFrameworkYamlPath = Path.Combine("SecurityScanning", "AutomationFrameworkYamls", "Baseline.yml");
        }

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

        var mountedDirectoryPath = DirectoryPaths.GetTempSubDirectoryPath(context.Id, "Zap");
        Directory.CreateDirectory(mountedDirectoryPath);

        var yamlFileName = Path.GetFileName(automationFrameworkYamlPath);

        File.Copy(
            automationFrameworkYamlPath,
            Path.Combine(mountedDirectoryPath, yamlFileName),
            overwrite: true);

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
            mountedDirectoryPath + ":/zap/wrk/:rw",
            "--tty",
            _zapImage,
            "zap.sh",
            "-cmd",
            "-autorun",
            "/zap/wrk/" + yamlFileName,
        });

        var stdErrBuffer = new StringBuilder();

        var result = await _docker
            .GetCommand(cliParameters)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _testOutputHelper.WriteLineTimestampedAndDebug(line)))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            // This is so no exception is thrown by CliWrap if the exit code is not 0.
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(_cancellationTokenSource.Token);
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
            await _restoreSemaphore.WaitAsync(token);

            if (!_wasPulled)
            {
                await _docker.ExecuteAsync(token, "pull", _zapImage);
                _wasPulled = true;
            }
        }
        finally
        {
            _restoreSemaphore.Release();
        }
    }
}
