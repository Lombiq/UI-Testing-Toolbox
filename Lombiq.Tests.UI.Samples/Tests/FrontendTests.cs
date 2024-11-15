using Lombiq.Tests.UI.Samples.Helpers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class FrontendTests : UITestBase
{
    public FrontendTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task ServerSideErrorOnLoadedPageShouldHaltTest() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
            },
            SetupHelpers.ConfigureFrontendSetupAsync);
}

// END OF TRAINING SECTION: Test headless Orchard Core with a frontend subprocess.
// NEXT STATION: Head over to Tests/JavascriptTests.cs.
