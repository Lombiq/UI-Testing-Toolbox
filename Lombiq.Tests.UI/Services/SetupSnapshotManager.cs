using Lombiq.Tests.UI.Constants;

namespace Lombiq.Tests.UI.Services
{
    public static class SetupSnapshotManager
    {
        private static readonly object _lock = new object();
        private static SynchronizingWebApplicationSnapshotManager _instance;

        public static SynchronizingWebApplicationSnapshotManager Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new SynchronizingWebApplicationSnapshotManager(Snapshots.DefaultSetupSnapshotPath);
                }
            }
        }
    }
}
