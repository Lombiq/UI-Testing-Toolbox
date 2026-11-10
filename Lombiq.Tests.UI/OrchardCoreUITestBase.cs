using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.GitHub;
using SixLabors.ImageSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

// These are not static fields because those shouldn't be used in generic types. See:
// https://rules.sonarsource.com/csharp/RSPEC-2743/.
internal static class OrchardCoreUITestBaseCounter
{
    public static SemaphoreSlim SnapshotCopySemaphoreSlim = new(1, 1);
    public static bool AppFolderCreated { get; set; }
}

public abstract partial class OrchardCoreUITestBase<TEntryPoint> : UITestBase
     where TEntryPoint : class
{
    private const string AppFolder = nameof(AppFolder);

    protected virtual Size StandardBrowserSize => CommonDisplayResolutions.Standard;
    protected virtual Size MobileBrowserSize => CommonDisplayResolutions.NhdPortrait;

    protected override WebApplicationInstanceFactory InstanceFactory { get; }

    protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper) =>
        InstanceFactory = (configuration, contextId) => new OrchardCoreInstance<TEntryPoint>(configuration, contextId);

    /// <summary>
    /// Adds the provided two test methods, to be executed with the browser size of <see cref="StandardBrowserSize"/>
    /// and then <see cref="MobileBrowserSize"/>.
    /// </summary>
    /// <param name="standardBrowserSizeTestAsync">
    /// When ran, the browser size is changed to <see cref="StandardBrowserSize"/>, and then this method is executed.
    /// </param>
    /// <param name="mobileBrowserSizeTestAsync">
    /// When ran, the browser size is changed to <see cref="MobileBrowserSize"/>, and then this method is executed. If
    /// this parameter is <see langword="null"/>, <paramref name="standardBrowserSizeTestAsync"/> is used for the mobile
    /// size as well.
    /// </param>
    protected OrchardCoreUITestBase<TEntryPoint> AddMultiSizeTest(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync = null)
    {
        ArgumentNullException.ThrowIfNull(standardBrowserSizeTestAsync);
        mobileBrowserSizeTestAsync ??= standardBrowserSizeTestAsync;

        AddTest(context =>
        {
            context.SetBrowserSize(StandardBrowserSize);
            return standardBrowserSizeTestAsync(context, isStandardSize: true);
        });

        AddTest(context =>
        {
            context.SetBrowserSize(MobileBrowserSize);
            return mobileBrowserSizeTestAsync(context, isStandardSize: false);
        });

        return this;
    }

    /// <summary>
    /// Executes test by starting the app from an existing SQLite database.
    /// </summary>
    /// <param name="customSnapshotFolderPath">
    /// The directory containing the SQLite file. If it's <see langword="null"/>, then the App_Data folder is used.
    /// </param>
    protected async Task UseExistingDatabaseAndRunAsync(string customSnapshotFolderPath = null)
    {
        var token = TestContext.Current.CancellationToken;
        await OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.WaitAsync(token);

        try
        {
            if (!OrchardCoreUITestBaseCounter.AppFolderCreated)
            {
                await DirectoryHelper.SafelyDeleteDirectoryIfExistsAsync(AppFolder, token);

                customSnapshotFolderPath ??= OrchardCoreDirectoryHelper.GetAppRootPath(typeof(TEntryPoint).Assembly.Location);
                await OrchardCoreDirectoryHelper.CopyAppFolderAsync(customSnapshotFolderPath, AppFolder, token);

                OrchardCoreUITestBaseCounter.AppFolderCreated = true;
            }
        }
        finally
        {
            OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.Release();
        }

        WithoutSetup();
        AddSyncConfiguration(configuration => configuration.SetupConfiguration.SetupSnapshotDirectoryPath = AppFolder);
        await RunAsync();
    }

    public override Task RunAsync()
    {
        AddSyncConfiguration(configuration => configuration.SetupConfiguration.SetupOperation = Setup);
        return base.RunAsync();
    }
}
