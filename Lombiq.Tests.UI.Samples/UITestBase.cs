using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Samples.Helpers;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples
{
    // This will be the base class for our UI test classes. Here we'll set up some common configuration.
    // Inheriting test classes is not mandatory but the approach is simple and effective.
    public class UITestBase : OrchardCoreUITestBase
    {
        // We somehow need to tell the UI Testing Toolbox where the assemblies of the app under test is (since it'll run
        // the app from the command line). We use a helper for that.
        protected override string AppAssemblyPath => WebAppConfigHelper
            .GetAbsoluteApplicationAssemblyPath("Lombiq.OSOCE.Web", "netcoreapp3.1");

        protected UITestBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // Two usual overloads. Note that we reference SetupHelpers.RunSetup as the setup operation: That's what will be
        // run when the first test executes. Until the setup is done, all other tests wait; then, they'll use the
        // snapshot created from the setup.
        // Do you use Auto Setup? No problem: Check out SetupHelpers.RunAutoSetup().
        // NEXT STATION: Check out SetupHelpers, then come back here!
        protected override Task ExecuteTestAfterSetupAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            ExecuteTestAsync(testAsync, browser, SetupHelpers.RunSetupAsync, changeConfigurationAsync);

        // You could wrap all your tests by providing a different delegate as the first parameter of ExecuteTestAsync()
        // and do something before or after they're executed but this is not always necessary.
        protected override Task ExecuteTestAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<UITestContext, Task<Uri>> setupOperation,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            base.ExecuteTestAsync(
                testAsync,
                browser,
                setupOperation,
                async configuration =>
                {
                    // You should always set the window size of the browser, otherwise the size will be random based on
                    // the settings of the given machine. However this is already handled as long as the
                    // context.Configuration.BrowserConfiguration.DefaultBrowserSize option is properly set. You can
                    // change it here but usually the default full HD is suitable.
                    configuration.BrowserConfiguration.DefaultBrowserSize = CommonDisplayResolutions.HdPlus;

                    // In headless mode, the browser's UI is not showing, it just runs in the background. This is what
                    // you want to use when running all tests, especially in a CI environment. During local
                    // troubleshooting you may want to turn this off so you can see in the browser what's happening.
                    // Hence the override of the default here.
                    // Apart from changing the code here, you can use a configuration file or environment variables, see
                    // the docs.
                    configuration.BrowserConfiguration.Headless =
                        TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration:Headless", defaultValue: false);

                    // There are event handlers that you can hook into. This is just one but check out the others in
                    // OrchardCoreConfiguration if you're interested.
                    configuration.OrchardCoreConfiguration.BeforeAppStart +=
                        (_, argumentsBuilder) =>
                        {
                            // This is quite handy! We're adding a configuration parameter when launching the app. This
                            // can be used to set configuration for configuration providers, see the docs:
                            // https://docs.orchardcore.net/en/latest/docs/reference/core/Configuration/.
                            // What's happening here is that we set the Lombiq Application Insights module's parameter
                            // to allow us to test it. We'll get back to this later when writing the actual test.
                            argumentsBuilder
                                .Add("--OrchardCore:Lombiq_Hosting_Azure_ApplicationInsights:EnableOfflineOperation")
                                .Add("true");

                            return Task.CompletedTask;
                        };

                    // Note that automatic HTML markup validation is enabled on every page change by default (you can
                    // disable it with the below config). With this, you can make sure that the HTML markup the app
                    // generates (also from content items) is valid. While the default settings for HTML validation are
                    // most possibly suitable for your projects, check out the HtmlValidationConfiguration class for
                    // what else you can configure. We've also added a .htmlvalidate.json file (note the Content Build
                    // Action) to further configure it.
                    ////configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;

                    // The UI Testing Toolbox can run several checks for the app even if you don't add explicit
                    // assertions: By default, the Orchard logs and the browser logs (where e.g. JavaScript errors show
                    // up) are checked and if there are any errors, the test will fail. You can also enable the checking
                    // of accessibility rules as we'll see later.
                    // Maybe not all of the default checks are suitable for you. Then it's simple to override them; here
                    // we change which log entries cause the tests to fail. We use the trick of making expected error
                    // messages not look like real errors.
                    configuration.AssertAppLogsAsync = async webApplicationInstance =>
                        (await webApplicationInstance.GetLogOutputAsync())
                        .ReplaceOrdinalIgnoreCase(
                            "|Lombiq.TrainingDemo.Services.DemoBackgroundTask|ERROR|Expected non-error",
                            "|Lombiq.TrainingDemo.Services.DemoBackgroundTask|EXPECTED_ERROR|Expected non-error")
                        .ShouldNotContain("|ERROR|");

                    if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);
                });
    }
}

// NEXT STATION: Phew, that was a lot of configuration (and we have just scratched the surface!). Let's see some actual
// tests: Go to Tests/BasicTests.cs.
