using Lombiq.Tests.UI.Extensions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// When you enable the "Shift Time - Shortcuts - Lombiq UI Testing Toolbox" feature, it replaces OC's stock ICLock
// implementation with the custom ShiftTimeClock class. You can use the ~/Lombiq.Tests.UI.Samples/ShiftTime/Set?days=...
// action to update the ShiftTimeClock.Shift property for the current tenant, which will trick any service that uses
// IClock into thinking you are in the future. This can be used to test such things as expirations and timeouts.
public class ShiftTimeTests : UITestBase
{
    public ShiftTimeTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task TimeShouldUpdate() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // You can enable the feature from recipe too, but if you only need it for specific tests, then you can
                // use this extension method.
                await context.EnableTimeShiftingAsync();

                await context.SignInDirectlyAsync();
            });
}
