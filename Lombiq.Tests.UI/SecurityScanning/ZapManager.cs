using Lombiq.HelpfulLibraries.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public sealed class ZapManager : IAsyncDisposable
{
    private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");

    private static bool _wasPulled;

    private CancellationTokenSource _cancellationTokenSource;

    public async Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            await _restoreSemaphore.WaitAsync(token);

            if (!_wasPulled)
            {
                // Need to use the weekly release because that's the one that has packaged scans migrated to Automation
                // Framework.
                await _docker.ExecuteAsync(token, "pull", "softwaresecurityproject/zap-weekly:20231113");

                _wasPulled = true;
            }
        }
        finally
        {
            _restoreSemaphore.Release();
        }
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
}
