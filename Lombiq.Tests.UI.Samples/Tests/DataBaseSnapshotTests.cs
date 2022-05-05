using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Constants;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// We can execute tests on already existing databases. To demo this, we will take a snapshot of the running application,
// and use that, because we don't have a pre-beaked database. Normally this feature is used to run tests when there is
// an already existing database, so you don't need to take a snapshot before using the
// "ExecuteTestFromExistingDBAsync()" method.
public class DatabaseSnapshotTests : UITestBase
{
    public DatabaseSnapshotTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Here, we set up the application, then we take a snapshot of it, then we use the
    // "ExecuteTestFromExistingDBAsync()" to run the test on that. Finally, we test the basic Orchard features to check
    // that the application was set up correctly.
    [Theory, Chrome]
    public Task BasicOrchardFeaturesShouldWorkWithExistingDatabase(Browser browser) =>
         ExecuteTestAsync(
                async context =>
                {
                    const string AppForDatabaseTestFolder = "AppForDatabaseTest";

                    await context.GoToSetupPageAndSetupOrchardCoreAsync(RecipeIds.BasicOrchardFeaturesTests);
                    await context.Application.TakeSnapshotAsync(AppForDatabaseTestFolder);

                    await ExecuteTestFromExistingDBAsync(
                         async context => await context.TestBasicOrchardFeaturesExceptSetupAsync(),
                         browser,
                         AppForDatabaseTestFolder);
                },
                browser);
}

// END OF TRAINING SECTION: Database snapshot tests.
