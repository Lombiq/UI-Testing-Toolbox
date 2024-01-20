using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Constants;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// The UI Testing Toolbox includes ready to use tests for some basic Orchard features as well. While the point of
// writing tests for your app is not really about testing Orchard itself but nevertheless it's useful to check if all
// the important features like login work - keep in mind that you can break these from your own code. So, here we run
// the whole test suite.
public class BasicOrchardFeaturesTests(ITestOutputHelper testOutputHelper) : UITestBase(testOutputHelper)
{
    // We could reuse the previously specified SetupHelpers.RecipeId const here but it's actually a different recipe for
    // this test.
    [Fact]
    public Task BasicOrchardFeaturesShouldWork() =>
        ExecuteTestAsync(context => context.TestBasicOrchardFeaturesAsync(RecipeIds.BasicOrchardFeaturesTests));
}

// END OF TRAINING SECTION: Basic Orchard features tests.
// NEXT STATION: Head over to Tests/EmailTests.cs.
