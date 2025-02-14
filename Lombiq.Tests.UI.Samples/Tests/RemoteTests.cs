using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// We recommend always running UI tests on the latest code of your app as part of your CI workflow. So, if anything got
// broken by a pull request, it should be readily visible. Such tests are self-contained and should not even need access
// to the internet.

// However, sometimes in addition to this, you also want to test remote apps available online, like running rudimentary
// smoke tests on your production app (e.g.: Can people still log in? Are payments still working?). You can also run
// such tests in your pre-production environment (like staging) before rolling it out the production. The UI Testing
// Toolbox also supports this. Check out the example below!

// Note how the test derives from RemoteUITestBase this time, not UITestBase. This is a generic base class for any
// remote test. However, if you're testing an app behind Cloudflare, you might see requests rejected with an HTTP 403
// due to Cloudflare's Bot Fight Mode; in that case, derive from CloudflareRemoteUITestBase instead after checking out
// its documentation on how to set it up.

// While there may be some common code between local and remote tests, they cannot be the same, and the common code must
// be in a project independent of the web app (which is not the case here for the sake of simplicity):
// - In local tests, you have a much lower-level access to the app: E.g., you can access the Orchard Core log, use
//   shortcuts with all kinds of backdoors, and you can communicate directly with the Orchard Core shell (not just via #spell-check-ignore-line
//   web APIs). You can also save a snapshot of the site's database and media. None of these are available (for a good
//   reason) when the app is deployed to a server: There, you interact with it just as any ordinary user.
// - For local tests, you need to build the web app and all its dependencies. For remote tests, you only need the test
//   project and the UI Testing Toolbox.
// - Some things are inherently different in a staging/production app, also on the UI. E.g., the app will load static
//   resources from a CDN, or the content will be more up-to-date.

public class RemoteTests : RemoteUITestBase
{
    public RemoteTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // The test itself is largely the same as all the local ones, but you need to provide a base URI.
    [Fact]
    public Task ExampleDotComShouldWork() =>
        ExecuteTestAsync(
            new Uri("https://example.com/"),
            context =>
            {
                // Assertions work as usual. Implicit assertions like HTML validation and accessibility checks work too,
                // and upon a failing assertion a failure dump is generated as you'd expect it.
                context.Get(By.CssSelector("h1")).Text.ShouldBe("Example Domain");
                context.Exists(By.LinkText("More information..."));

                // Note that due to a remote app not being under our control, some things are not supported. E.g., you
                // can't access the Orchard Core logs, or use shortcuts (the *Directly methods).
            },
            configuration => configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false);
}

// END OF TRAINING SECTION: Remote tests.
// NEXT STATION: Head over to Tests/ShiftTimeTests.cs.
