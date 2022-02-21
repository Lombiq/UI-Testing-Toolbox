using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // We'll see some simpler tests as a start. Each of them will teach us important concepts.
    public class BasicTests : UITestBase
    {
        public BasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // Checking that everything is OK with the homepage as an anonymous user. Note the attributes: [Theory] is
        // necessary for xUnit, while [Chrome] is an input parameter of the test. The latter is an important concept:
        // You can create so-called data-driven tests. See here for more info:
        // https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/.
        [Theory, Chrome]
        public Task AnonymousHomePageShouldExist(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    // Is the title correct?
                    context
                        .Get(By.ClassName("navbar-brand"))
                        .Text
                        .ShouldBe("Lombiq's Open-Source Orchard Core Extensions - UI Testing");

                    // Are we logged out?
                    (await context.GetCurrentUserNameAsync()).ShouldBeNullOrEmpty();
                },
                browser);

        // Let's click around now. The login page is quite important, so let's make sure it works. While it's an Orchard
        // feature, and thus not necessarily something we want to test, our custom code can break it in various ways.
        [Theory, Chrome]
        public Task LoginShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    // The UI Testing Toolbox has an immense amount of helpers and shortcuts. This one lets you navigate
                    // to any URL.
                    await context.GoToRelativeUrlAsync("/Login");

                    // Let's fill out the login form. In UI tests, nothing is certain. If you fill out a form it's not
                    // actually sure that the values are indeed there! To make things more reliable, we've added a lot
                    // of useful methods like FillInWithRetries().
                    await context.FillInWithRetriesAsync(By.Id("UserName"), DefaultUser.UserName);
                    await context.FillInWithRetriesAsync(By.Id("Password"), DefaultUser.Password);

                    // Even clicking can be unreliable thus we have a helper for that too.
                    context.ClickReliablyOnSubmit();

                    // At this point we should be logged in. So let's use a shortcut (from the Lombiq.Tests.UI.Shortcuts
                    // module) to see if it indeed happened.
                    (await context.GetCurrentUserNameAsync()).ShouldBe(DefaultUser.UserName);

                    // Note that if you want the user to be logged in for the test (instead of testing the login feature
                    // itself), you don't need to log in via the login form every time: That would be slow and you'd
                    // test the login process multiple times. Use context.SignInDirectly() instead. Check out the
                    // ShortcutsShouldWork test below.
                },
                browser);

        // Let's see if turning features on and off breaks something. Keep in mind that the Orchard logs are checked
        // automatically so if there's an exception or anything, the test will fail.
        [Theory, Chrome]
        public Task TogglingFeaturesShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context.ExecuteAndAssertTestFeatureToggleAsync(),
                browser,
                // You can change the configuration even for each test.
                configuration =>
                    // By default, apart from some commonly known exceptions, the browser log should be empty. However,
                    // ExecuteAndAssertTestFeatureToggle() causes a 404 so we need to make sure not to fail on that.
                    configuration.AssertBrowserLog =
                        messages =>
                            {
                                var messagesWithoutToggle = messages.Where(message =>
                                    !message.IsNotFoundMessage(ShortcutsUITestContextExtensions.FeatureToggleTestBenchUrl));
                                OrchardCoreUITestExecutorConfiguration.AssertBrowserLogIsEmpty(messagesWithoutToggle);
                            });

        // Let's see a couple more useful shortcuts in action.
        [Theory, Chrome]
        public Task ShortcutsShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    // If you need an authenticated user but you aren't testing the login specifically then you can use
                    // this shortcut to authenticate (note that you can specify a different user in an argument too):
                    await context.SignInDirectlyAsync();

                    // You know this shortcut already:
                    (await context.GetCurrentUserNameAsync()).ShouldBe(DefaultUser.UserName);

                    // If you want to add some sample content in just one test, or change some Orchard configuration
                    // quickly, then defining those in a recipe and executing it will come handy:
                    await context.ExecuteRecipeDirectlyAsync("Lombiq.JsonEditor.Sample");

                    // Retrieving some in-depth details about the app.
                    var info = await context.GetApplicationInfoAsync();
                    // Where is the app's current instance running from?
                    _testOutputHelper.WriteLineTimestampedAndDebug("App root: " + info.AppRoot);

                    // If you want a feature to be enabled or disabled just for one test, you can use shortcuts too:
                    await context.EnableFeatureDirectlyAsync("OrchardCore.HealthChecks");
                },
                browser);

        // Let's play a bit with Lombiq's Azure Application Insights module: It allows you to easily collect telemetry
        // in Application Insights. Since it sends data to Azure, i.e. an external system, we should never use it during
        // UI testing since tests should be self-contained and only test the app. However, it would be still nice to at
        // least have some idea that the module works: Thus we've built an offline mode into it, what we turned on back
        // in UITestBase. Thus we can check at least that.
        [Theory, Chrome]
        public Task ApplicationInsightsTrackingShouldBePresent(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    // Now there's a bit of a pickle though: The Lombiq Privacy module is also enabled from the test
                    // recipe and shows its privacy consent banner. For tracking to be enabled, even in offline mode,
                    // the user needs to give consent. This is what we do now:
                    await context.ClickReliablyOnAsync(By.Id("privacy-consent-accept-button"));
                    context.Refresh();

                    // In offline mode, the module adds an appInsights variable that we can check. So let's execute some
                    // JavaScript in the browser.
                    var appInsightsExist = context
                        .ExecuteScript("return window.appInsights === 'enabled'") as bool?;

                    // Our custom message helps debugging, otherwise from the test output you could only tell that a
                    // a value should be true but is false which is less than helpful.
                    appInsightsExist.ShouldBe(expected: true, "The Application Insights module is not working or is not in offline mode.");
                },
                browser);
    }
}

// END OF TRAINING SECTION: UI Testing Toolbox basics.
// NEXT STATION: Head over to Tests/BasicOrchardFeaturesTests.cs.
