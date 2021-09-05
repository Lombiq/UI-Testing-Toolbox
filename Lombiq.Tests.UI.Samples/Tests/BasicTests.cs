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
                context =>
                {
                    // Is the title correct?
                    context
                        .Get(By.ClassName("navbar-brand"))
                        .Text
                        .ShouldBe("Lombiq's Open-Source Orchard Core Extensions - UI Testing");

                    // Are logged out?
                    context.GetCurrentUserName().ShouldBeNullOrEmpty();
                },
                browser);

        // Let's click around now. The login page is quite important, so let's make sure it works. While it's an Orchard
        // feature, and thus not necessarily something we want to test, our custom code can break it in various ways.
        [Theory, Chrome]
        public Task LoginShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    // The UI Testing Toolbox has an immense amount of helpers and shortcuts. This one lets you navigate
                    // to any URL.
                    context.GoToRelativeUrl("/Login");

                    // Let's fill out the login form. In UI tests, nothing is certain. If you fill out a form it's not
                    // actually sure that the values are indeed there! To make things more reliably, we've added a lot
                    // of useful methods like FillInWithRetries().
                    context.FillInWithRetries(By.Id("UserName"), DefaultUser.UserName);
                    context.FillInWithRetries(By.Id("Password"), DefaultUser.Password);

                    // Even clicking can be unreliable thus we have a helper for that too.
                    context.ClickReliablyOnSubmit();

                    // At this point we should be logged in. So let's use a shortcut (from the Lombiq.Tests.UI.Shortcuts
                    // module) to see if it indeed happened.
                    context.GetCurrentUserName().ShouldBe(DefaultUser.UserName);

                    // Note that if you want the user to be logged in for the test, you don't need to log in via the
                    // login form every time: That would be slow and you'd test the login process multiple times. Use
                    // context.SignInDirectly() instead.
                },
                browser);

        // Let's see if turning features on and off breaks something. Keep in mind that the Orchard logs are checked
        // automatically so if there's an exception or anything, the test will fail.
        [Theory, Chrome]
        public Task TogglingFeaturesShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context.ExecuteAndAssertTestFeatureToggle(),
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
                            }
                            );

        // Let's play a bit with Lombiq's Azure Application Insights module: It allows you to easily collect telemetry
        // in Application Insights. Since it sends data to Azure, i.e. an external system, we should never use it during
        // UI testing since tests should be self-contained and only test the app. However, it would be still nice to at
        // least have some idea that the module works: Thus we've built an offline mode into it, what we turned on back
        // in UITestBase. Thus we can check at least that.
        [Theory, Chrome]
        public Task ApplicationInsightsTrackingShouldBePresent(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    // In offline mode, the module adds an appInsights variable that we can check. So let's execute some
                    // JavaScript in the browser.
                    var appInsightsExist = context
                        .ExecuteScript("return window.appInsights === 'enabled'") as bool?;
                    appInsightsExist.ShouldBe(true);
                },
                browser);
    }
}

// END OF TRAINING SECTION: UI Testing Toolbox basics.
// NEXT STATION: Head over to Tests/EmailTests.cs.
