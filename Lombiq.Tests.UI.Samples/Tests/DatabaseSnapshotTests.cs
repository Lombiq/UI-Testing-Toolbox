using Lombiq.Tests.UI.BasicOrchardFeaturesTesting;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Samples.Constants;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// We can execute tests on already existing databases. In this demo we first set up a fresh application and create a
// snapshot the next test can use, to keep everything self-contained. Normally the database snapshot would be created
// elsewhere or manually committed to the repository.
public class DatabaseSnapshotTests : UITestBase
{
    private readonly string _appForDatabaseTestFolder = Path.Combine("Temp", "AppForDatabaseTest");

    public DatabaseSnapshotTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Here, we set up the application, take a snapshot of it, then call "UseExistingDatabaseAndRunAsync()" to run a new
    // test on the stored database. There, we test the basic Orchard Core features to verify that everything works.
    [Fact]
    public async Task BasicOrchardFeaturesShouldWorkWithExistingDatabase()
    {
        // Use the setup with only the basic OC features, then store the database. Normally you don't need to do this
        // within the test.
        UseSetup(context => context.GoToSetupAndSetupOrchardCoreAsync(RecipeIds.BasicOrchardFeaturesTests));
        AddTest(context => context.Application.TakeSnapshotAsync(_appForDatabaseTestFolder));
        await RunAsync();

        // Here we use "SetTest", the less-used sibling of "AddTest" which first clears out the testing pipeline. This
        // is necessary to remove the test step declared above. We don't have to do the same for the setup, because
        // "UseExistingDatabaseAndRunAsync" automatically sets the setup operation to "null".
        SetTest(context => context.TestBasicOrchardFeaturesAsync(new OrchardCoreSetupParameters { SkipSetup = true }));
        await UseExistingDatabaseAndRunAsync(_appForDatabaseTestFolder);
    }
}

// END OF TRAINING SECTION: Database snapshot tests.
// NEXT STATION: Head over to Tests/MultiBrowserTests.cs.
