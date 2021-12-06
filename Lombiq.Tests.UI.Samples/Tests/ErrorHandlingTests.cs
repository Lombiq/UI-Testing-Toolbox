using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // Sometimes errors are expected. Let's check out what can be done with them!
    public class ErrorHandlingTests : UITestBase
    {
        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // It's easier to diagnose a test failure if you know whether an element is missing because there something is
        // actually missing or there was a server-side error. The below test visits a page where the action method
        // throws an exception.
        [Theory, Chrome]
        public Task ErrorOnLoadedPageShouldHaltTest(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    try
                    {
                        context.GoToErrorPageDirectly();

                        // This point should be unreachable because logs are automatically asserted after a page load.
                        throw new InvalidOperationException("The log assertion didn't happen after page load!");
                    }
                    catch (PageChangeAssertionException)
                    {
                        // Remove logs to have a clean slate.
                        foreach (var log in context.Application.GetLogs()) log.Remove();
                    }
                },
                browser);
    }
}

// END OF TRAINING SECTION: Error Handling.
