using Lombiq.Tests.UI.Attributes;
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
                //await context.SwitchToInteractiveAsync();
                //await zapManager.StartInstanceAsync("https://localhost:44335/");
                await context.ZapManager.RunSecurityScanAsync(context, context.Scope.BaseUri);
            },
            browser);
}

// END OF TRAINING SECTION: Security scanning.
