using Lombiq.Tests.UI.BasicOrchardFeaturesTesting;
using Lombiq.Tests.UI.Samples.Constants;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// The UI Testing Toolbox includes ready to use tests for some basic Orchard features as well. While the point of
// writing tests for your app is not really about testing Orchard itself but nevertheless it's useful to check if all
// the important features like login work. Keep in mind that you can break these from your own code! So, here we run
// the whole test suite.
public class BasicOrchardFeaturesTests : UITestBase
{
    public BasicOrchardFeaturesTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // We could reuse the previously specified SetupHelpers.RecipeId const here but it's actually a different recipe for
    // this test. We also call "WithoutSetup" to undo the "UseSetup(SetupHelpers.RunSetupAsync)" in the parent class.
    [Fact]
    public Task BasicOrchardFeaturesShouldWork() =>
        WithoutSetup().RunAsync(context => context.TestBasicOrchardFeaturesAsync(RecipeIds.BasicOrchardFeaturesTests));
}

// END OF TRAINING SECTION: Basic Orchard features tests.
// NEXT STATION: Head over to Tests/EmailTests.cs.
