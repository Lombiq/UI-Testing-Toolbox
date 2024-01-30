using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Up until now, all of our tests were run via Chrome. However, it's important that you can run tests with any of the
// other supported browsers too, even running a test with all of them at once! This class shows you how.
public class MultiBrowserTests : UITestBase
{
    public MultiBrowserTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Remember that back in BasicTests we had AnonymousHomePageShouldExist()? We have similar super-simple tests here,
    // just demonstrating how to drive different browsers.

    // First, let's see a test using Edge. While the default browser is Chrome if you don't set anything, all
    // ExecuteTest* methods can also accept a browser, if you want to use a different one.
    [Fact]
    public Task AnonymousHomePageShouldExistWithEdge() =>
        ExecuteTestAfterSetupAsync(NavbarIsCorrect, Browser.Edge);

    // This test is now marked not with the [Fact] attribute but [Theory]. With it, you can create so-called data-driven
    // tests. [Chrome] and [Edge] are input parameters of the test, and thus in effect, you have now two tests:
    // AnonymousHomePageShouldExistMultiBrowser once with Chrome, and once with Edge. See here for more info:
    // https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/.
    [Theory, Chrome, Edge]
    public Task AnonymousHomePageShouldExistMultiBrowser(Browser browser) =>
        ExecuteTestAfterSetupAsync(NavbarIsCorrect, browser);

    // You can also set the browser for all tests at once in UITestBase. Check it out: Where there's some default config
    // now, you could also have the following code, for example:
    //// configuration.BrowserConfiguration.Browser = Browser.Edge;

    private static void NavbarIsCorrect(UITestContext context) =>
        context.Get(By.ClassName("navbar-brand")).Text.ShouldBe("Lombiq's OSOCE - UI Testing");
}

// END OF TRAINING SECTION: Multi-browser tests.
// NEXT STATION: Head over to Tests/BasicVisualVerificationTests.cs.
