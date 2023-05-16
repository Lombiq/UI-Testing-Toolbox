using Lombiq.Tests.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public delegate Task<(UITestContext Context, Uri ResultUri)> AppInitializer();

/// <summary>
/// Service for transparently running operations on a web application and snapshotting them just a single time, so the
/// snapshot can be used for further operations. Create a single instance of this for every kind of snapshot (like an
/// Orchard application already set up).
/// </summary>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "This is because SemaphoreSlim but it's not actually necessary to dispose in this case: " +
        "https://stackoverflow.com/questions/32033416/do-i-need-to-dispose-a-semaphoreslim. Making this class " +
        "IDisposable would need disposing static members above on app shutdown, which is unreliable.")]
public class SynchronizingWebApplicationSnapshotManager
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _snapshotDirectoryPath;

    private Uri _resultUri;
    private bool _snapshotCreated;

    public SynchronizingWebApplicationSnapshotManager(string snapshotDirectoryPath) => _snapshotDirectoryPath = snapshotDirectoryPath;

    public async Task<Uri> RunOperationAndSnapshotIfNewAsync(AppInitializer appInitializer)
    {
        DebugHelper.WriteLineTimestamped($"Entering SynchronizingWebApplicationSnapshotManager semaphore for {_snapshotDirectoryPath}.");

        await _semaphore.WaitAsync();
        try
        {
            if (_snapshotCreated) return _resultUri;

            DebugHelper.WriteLineTimestamped("Creating snapshot.");

            // Always start the current test run with a fresh snapshot.
            DirectoryHelper.SafelyDeleteDirectoryIfExists(_snapshotDirectoryPath);

            var result = await appInitializer();
            await result.Context.Application.TakeSnapshotAsync(_snapshotDirectoryPath);
            await result.Context.Application.ResumeAsync();

            DebugHelper.WriteLineTimestamped("Finished snapshot.");

            // At the end so if any exception happens above then it won' be mistakenly set to true.
            _snapshotCreated = true;

            return _resultUri ??= result.ResultUri;
        }
        finally
        {
            DebugHelper.WriteLineTimestamped($"Exiting SynchronizingWebApplicationSnapshotManager semaphore for {_snapshotDirectoryPath}.");
            _semaphore.Release();
        }
    }
}
