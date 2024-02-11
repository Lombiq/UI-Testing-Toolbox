using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// We recommend always running UI tests on the latest code of your app as part of your CI workflow. So, if anything got
// broken by a pull request, it should be readily visible. Such tests are self-contained and should not even need access
// to the internet.

// However, sometimes in addition to this, you also want to test remote apps available online, like running rudimentary
// smoke tests on your production app (e.g.: Can people still log in? Are payments still working?). The UI Testing
// Toolbox also supports this. Check out the example below!

// Note how the test derives from RemoteUITestBase this time, not UITestBase.
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
