using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// The UI Testing Toolbox includes ready to use tests for some basic Orchard features as well. While the point of
// writing tests for your app is not really about testing Orchard itself but nevertheless it's useful to check if all
// the important features like login work - keep in mind that you can break these from your own code. So, here we run
// the whole test suite.
public class BasicOrchardFeaturesTests : UITestBase
{
    // We could reuse the previously specified SetupHelpers.RecipeId const here but it's actually a different recipe for
    // these tests.
    private const string BasicOrchardFeaturesTestsRecipeId = "Lombiq.OSOCE.BasicOrchardFeaturesTests";

    public BasicOrchardFeaturesTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task BasicOrchardFeaturesShouldWork(Browser browser) =>
        ExecuteTestAsync(
            context => context.TestBasicOrchardFeaturesAsync(BasicOrchardFeaturesTestsRecipeId),
            browser);

    // For testing, we can use the already existing databases. Here, we set up the application, then we take a snapshot
    // of it, then we use the "ExecuteTestFromExistingDBAsync()" to run the test on that. Then we test the basic Orchard
    // features as we did above.
    [Theory, Chrome]
    public Task BasicOrchardFeaturesShouldWorkWithExistingDBSetup(Browser browser) =>
         ExecuteTestAsync(
                async context =>
                {
                    const string AppForDataBaseTestFolder = "AppForDataBaseTest";

                    await context.GoToSetupPageAndSetupOrchardCoreAsync(BasicOrchardFeaturesTestsRecipeId);
                    await context.Application.TakeSnapshotAsync(AppForDataBaseTestFolder);

                    await ExecuteTestFromExistingDBAsync(
                         async context => await context.TestBasicOrchardFeaturesExceptSetupAsync(),
                         browser,
                         Directory.GetCurrentDirectory() + "//" + AppForDataBaseTestFolder);
                },
                browser);
}

// END OF TRAINING SECTION: Basic Orchard features tests.
// NEXT STATION: Head over to Tests/EmailTests.cs.
