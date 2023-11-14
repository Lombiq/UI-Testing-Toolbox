using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class SecurityScanningTests : UITestBase
{
    public SecurityScanningTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task SecurityScanShouldPass(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                var zapManager = new ZapManager();
                await zapManager.StartInstanceAsync();
            },
            browser);
}

// END OF TRAINING SECTION: Security scanning.
