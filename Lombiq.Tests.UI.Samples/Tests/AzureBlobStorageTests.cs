using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Extensions;
using Lombiq.Tests.UI.Services;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Up until now the Orchard app always used the default local Media storage for managing Media files. However, you may
// use Azure Blob Storage in production. You can also test your app with it!
public class AzureBlobStorageTests : UITestBase
{
    public AzureBlobStorageTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Here we have basically two of the same tests as in BasicTests but now we're using Azure Blob Storage as the
    // site's Media storage. If they still work and there are no errors in the log then the app works with Azure Blob
    // Storage too.
    [Fact]
    public Task AnonymousHomePageShouldExistWithAzureBlobStorage() =>
        ExecuteTestAfterSetupAsync(
            context => context.CheckIfAnonymousHomePageExistsAsync(),
            // Note the configuration! We could also set this globally in UITestBase. You'll need an accessible Azure
            // Blob Storage account. For testing we recommend the Azurite emulator
            // (https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite) that can be used from tests
            // without any further configuration.
            configuration => configuration.UseAzureBlobStorage = true);

    [Fact]
    public Task TogglingFeaturesShouldWorkWithAzureBlobStorage() =>
        ExecuteTestAfterSetupAsync(
            context => context.ExecuteAndAssertTestFeatureToggleAsync(),
            configuration =>
            {
                configuration.UseAzureBlobStorage = true;

                configuration.AssertBrowserLog =
                    logEntries =>
                    {
                        var messagesWithoutToggle = logEntries.Where(logEntry =>
                            !logEntry.IsNotFoundLogEntry(ShortcutsUITestContextExtensions.FeatureToggleTestBenchUrl));
                        OrchardCoreUITestExecutorConfiguration.AssertBrowserLogIsEmpty(messagesWithoutToggle);
                    };
            });
}

// END OF TRAINING SECTION: Using Azure Blob Storage.
// NEXT STATION: Head over to Tests/ErrorHandlingTests.cs.
