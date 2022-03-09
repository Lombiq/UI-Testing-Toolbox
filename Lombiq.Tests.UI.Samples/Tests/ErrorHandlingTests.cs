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
        public Task ServerSideErrorOnLoadedPageShouldHaltTest(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    try
                    {
                        await context.GoToErrorPageDirectlyAsync();

                        // This point should be unreachable because Orchard logs are automatically asserted after a
                        // page load.
                        throw new InvalidOperationException("The log assertion didn't happen after page load!");
                    }
                    catch (PageChangeAssertionException)
                    {
                        // Remove logs to have a clean slate.
                        foreach (var log in context.Application.GetLogs()) log.Remove();
                        context.ClearHistoricBrowserLog();
                    }
                },
                browser);

        // You can interact with the browser log and its history as well. E.g. 404s and JS exceptions show up in the
        // browser log.
        [Theory, Chrome]
        public Task ClientSideErrorOnLoadedPageShouldHaltTest(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    try
                    {
                        await context.GoToRelativeUrlAsync("/this-does-not-exist");

                        // This point should be unreachable because browser logs are automatically asserted after a
                        // page load.
                        throw new InvalidOperationException("The log assertion didn't happen after page load!");
                    }
                    catch (PageChangeAssertionException)
                    {
                        // Remove logs to have a clean slate.
                        context.ClearHistoricBrowserLog();
                    }
                },
                browser);
    }
}

// END OF TRAINING SECTION: Error handling.
// NEXT STATION: Head over to Tests/MonkeyTests.cs.
