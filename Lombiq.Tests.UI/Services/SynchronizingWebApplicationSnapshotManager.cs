using Lombiq.Tests.UI.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public delegate Task<(UITestContext Context, Uri ResultUri)> AppInitializer();


    /// <summary>
    /// Service for transparently running operations on a web application and snapshotting them just a single time, so
    /// the snapshot can be used for further operations. Create a single instance of this for every kind of snapshot
    /// (like an Orchard application already set up).
    /// </summary>
    public class SynchronizingWebApplicationSnapshotManager
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _snapshotDirectoryPath;

        private Uri _resultUri;
        private bool _snapshotCreated;


        public SynchronizingWebApplicationSnapshotManager(string snapshotDirectoryPath) => _snapshotDirectoryPath = snapshotDirectoryPath;


        public async Task<Uri> RunOperationAndSnapshotIfNew(AppInitializer appInitializer)
        {
            DebugHelper.WriteTimestampedLine("Entering SynchronizingWebApplicationSnapshotManager semaphore.");

            await _semaphore.WaitAsync();
            try
            {
                if (_snapshotCreated) return _resultUri;

                DebugHelper.WriteTimestampedLine("Creating snapshot.");

                // Always start the current test run with a fresh snapshot.
                DirectoryHelper.SafelyDeleteDirectoryIfExists(_snapshotDirectoryPath);

                var result = await appInitializer();
                await result.Context.Application.TakeSnapshot(_snapshotDirectoryPath);
                await result.Context.Application.Resume();

                DebugHelper.WriteTimestampedLine("Finished snapshot.");

                // At the end so if any exception happens above then it won' be mistakenly set to true.
                _snapshotCreated = true;

                return _resultUri ??= result.ResultUri;
            }
            finally
            {
                DebugHelper.WriteTimestampedLine("Exiting SynchronizingWebApplicationSnapshotManager semaphore.");
                _semaphore.Release();
            }
        }
    }
}
