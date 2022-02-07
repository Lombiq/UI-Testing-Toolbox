using Lombiq.Tests.UI.Constants;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public delegate Task BeforeSetupHandler(OrchardCoreUITestExecutorConfiguration configuration);

    /// <summary>
    /// Configuration for the initial setup of an Orchard Core app.
    /// </summary>
    public class OrchardCoreSetupConfiguration
    {
        /// <summary>
        /// Gets or sets the Orchard setup operation so the result can be snapshot and used in subsequent tests.
        /// WARNING: It's highly recommended to put assertions at the end to detect setup issues and fail tests early
        /// (since they can't be reliable after an improper setup). If configured, automatic log assertions will be
        /// executed after the setup operation too. Also see <see cref="FastFailSetup"/>.
        /// </summary>
        public Func<UITestContext, Task<Uri>> SetupOperation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if a specific setup operations fails and exhausts <see
        /// cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/> globally then all tests using the same
        /// operation should fail immediately, without attempting to run it again. If set to <see langword="false"/>
        /// then every test will retry the setup until each tests' <see
        /// cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/> allows. If set to <see langword="true"/> then
        /// the setup operation will only be retried <see cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/>
        /// times (as set by the first test running that operation) altogether. Defaults to <see langword="true"/>.
        /// </summary>
        public bool FastFailSetup { get; set; } = true;

        public string SetupSnapshotDirectoryPath { get; set; } = Snapshots.DefaultSetupSnapshotDirectoryPath;

        public BeforeSetupHandler BeforeSetup { get; set; }
    }
}
