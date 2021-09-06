using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // Up until now the Orchard app always used the default local Media storage for managing Media files. However, you
    // may use Azure Blob Storage in production. You can also test your app with it!
    public class AzureBlobStorageTests : UITestBase
    {
        public AzureBlobStorageTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // Here we have basically two of the same tests as in BasicTests but now we're using Azure Blob Storage as the
        // site's Media storage. If they still work and there are no errors in the log then the app works with Azure
        // Blob Storage too.
        [Theory, Chrome]
        public Task AnonymousHomePageShouldExistWithAzureBlobStorage(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context
                    .Get(By.ClassName("navbar-brand"))
                    .Text
                    .ShouldBe("Lombiq's Open-Source Orchard Core Extensions - UI Testing"),
                browser,
                // Note the configuration! We could also set this globally in UITestBase.
                configuration => configuration.UseAzureBlobStorage = true);

        [Theory, Chrome]
        public Task TogglingFeaturesShouldWorkWithSqlServer(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context.ExecuteAndAssertTestFeatureToggle(),
                browser,
                configuration =>
                {
                    configuration.UseAzureBlobStorage = true;

                    configuration.AssertBrowserLog =
                        messages =>
                        {
                            var messagesWithoutToggle = messages.Where(message =>
                                !message.IsNotFoundMessage(ShortcutsUITestContextExtensions.FeatureToggleTestBenchUrl));
                            OrchardCoreUITestExecutorConfiguration.AssertBrowserLogIsEmpty(messagesWithoutToggle);
                        };
                });
    }
}

// END OF TRAINING SECTION: Using Azure Blob Storage.
