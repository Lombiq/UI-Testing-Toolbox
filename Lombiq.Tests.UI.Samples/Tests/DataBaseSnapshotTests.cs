using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Constants;
using Lombiq.Tests.UI.Services;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// We can execute test on already existing databases. To test this, we will take a snapshot of the running application,
// and use that.
public class DataBaseSnapshotTests : UITestBase
{
    public DataBaseSnapshotTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Here, we set up the application, then we take a snapshot of it, then we use the
    // "ExecuteTestFromExistingDBAsync()" to run the test on that. Then we test the basic Orchard features to check that
    // the application was set up correctly.
    [Theory, Chrome]
    public Task BasicOrchardFeaturesShouldWorkWithExistingDBSetup(Browser browser) =>
         ExecuteTestAsync(
                async context =>
                {
                    const string AppForDataBaseTestFolder = "AppForDataBaseTest";

                    await context.GoToSetupPageAndSetupOrchardCoreAsync(RecipeIds.BasicOrchardFeaturesTests);
                    await context.Application.TakeSnapshotAsync(AppForDataBaseTestFolder);

                    await ExecuteTestFromExistingDBAsync(
                         async context => await context.TestBasicOrchardFeaturesExceptSetupAsync(),
                         browser,
                         Directory.GetCurrentDirectory() + "//" + AppForDataBaseTestFolder);
                },
                browser);
}

// END OF TRAINING SECTION: Database snapshot tests.
