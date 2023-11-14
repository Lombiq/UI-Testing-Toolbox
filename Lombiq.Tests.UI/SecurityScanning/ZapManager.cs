using Lombiq.HelpfulLibraries.Cli;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public sealed class ZapManager : IAsyncDisposable
{
    // Need to use the weekly release because that's the one that has packaged scans migrated to Automation Framework.
    private const string _zapImage = "softwaresecurityproject/zap-weekly:20231113";

    private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");

    private static bool _wasPulled;

    private CancellationTokenSource _cancellationTokenSource;

    public async Task RunSecurityScanAsync(Uri startUri)
    {
        await EnsureInitializedAsync();

        // Explanation on the arguments used below:
        // - --add-host and --network host: Lets us connect to the host OS's localhost, where the OC app runs, with
        //   https://localhost. See https://stackoverflow.com/a/24326540/220230. --network host serves the same, but
        //   only works under Linux. See https://docs.docker.com/engine/reference/commandline/run/#network and
        //   https://docs.docker.com/network/drivers/host/.

        // Running a ZAP desktop in the browser with Webswing with the same config:
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
            _zapImage,
            "zap-baseline.py",
            "-t",
            startUri.ToString(),
        });

        var result = await _docker.ExecuteAndGetOutputAsync(
            cliParameters,
            additionalExceptionText: null,
            _cancellationTokenSource.Token);
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
