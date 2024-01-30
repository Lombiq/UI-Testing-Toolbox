using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// We'll see some simpler tests as a start. Each of them will teach us important concepts.
public class BasicTests : UITestBase
{
    public BasicTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Checking that everything is OK with the homepage as an anonymous user. Note the [Fact] attribute: it's necessary
    // for xUnit.
    // Note that by default, tests are run via Chrome. Check out MultiBrowserTests for samples on using other browsers.
    [Fact]
    public Task AnonymousHomePageShouldExist() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // Is the title correct?
                context
                    .Get(By.ClassName("navbar-brand"))
                    .Text
                    .ShouldBe("Lombiq's OSOCE - UI Testing");

                // Are we logged out?
                (await context.GetCurrentUserNameAsync()).ShouldBeNullOrEmpty();
            });

    // Let's click around now. The login page is quite important, so let's make sure it works. While it's an Orchard
    // feature, and thus not necessarily something we want to test, our custom code can break it in various ways.
    [Fact]
    public Task LoginShouldWork() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // The UI Testing Toolbox has an immense amount of helpers and shortcuts. This one lets you navigate to
                // any URL.
                await context.GoToRelativeUrlAsync("/Login");

                // Let's fill out the login form. In UI tests, nothing is certain. If you fill out a form it's not
                // actually sure that the values are indeed there! To make things more reliable, we've added a lot of
                // useful methods like FillInWithRetriesAsync().
                await context.FillInWithRetriesAsync(By.Id("UserName"), DefaultUser.UserName);
                await context.FillInWithRetriesAsync(By.Id("Password"), DefaultUser.Password);

                // Even clicking can be unreliable thus we have a helper for that too.
                await context.ClickReliablyOnSubmitAsync();

                // At this point we should be logged in. So let's use a shortcut (from the Lombiq.Tests.UI.Shortcuts
                // module) to see if it indeed happened.
                (await context.GetCurrentUserNameAsync()).ShouldBe(DefaultUser.UserName);

                // Note that if you want the user to be logged in for the test (instead of testing the login feature
                // itself), you don't need to log in via the login form every time: That would be slow and you'd test
                // the login process multiple times. Use context.SignInDirectly() instead. Check out the
                // ShortcutsShouldWork test below.
            });

    // Let's see if turning features on and off breaks something. Keep in mind that the Orchard logs are checked
    // automatically so if there's an exception or anything, the test will fail.
    [Fact]
    public Task TogglingFeaturesShouldWork() =>
        ExecuteTestAfterSetupAsync(
            context => context.ExecuteAndAssertTestFeatureToggleAsync(),
            // You can change the configuration even for each test.
            configuration =>
                // By default, apart from some commonly known exceptions, the browser log should be empty. However,
                // ExecuteAndAssertTestFeatureToggle() causes a 404 so we need to make sure not to fail on that.
                configuration.AssertBrowserLog =
                    logEntries =>
                        {
                            var messagesWithoutToggle = logEntries.Where(logEntry =>
                                !logEntry.IsNotFoundLogEntry(ShortcutsUITestContextExtensions.FeatureToggleTestBenchUrl));
                            OrchardCoreUITestExecutorConfiguration.AssertBrowserLogIsEmpty(messagesWithoutToggle);
                        });

    // Let's see a couple more useful shortcuts in action.
    [Fact]
    public Task ShortcutsShouldWork() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // If you need an authenticated user but you aren't testing the login specifically then you can use this
                // shortcut to authenticate (note that you can specify a different user in an argument too):
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
            });
}

// END OF TRAINING SECTION: UI Testing Toolbox basics.
// NEXT STATION: Head over to Tests/BasicOrchardFeaturesTests.cs.
