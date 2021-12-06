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
        protected Task ExecuteTestAfterSetupAsync(
            Action<UITestContext> test,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteTestAsync(test, browser, SetupHelpers.RunSetup, changeConfiguration);

        protected override Task ExecuteTestAsync(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Uri> setupOperation = null,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            base.ExecuteTestAsync(
                context =>
                {
                    // Setting the browser size at the beginning of each test. As mentioned in SetupHelpers, this is
                    // quite important for reproducible results.
                    context.SetStandardBrowserSize();

                    test(context);
                },
                browser,
                setupOperation,
                configuration =>
                {
                    // In headless mode, the browser's UI is not showing, it just runs in the background. This is what
                    // you want to use when running all tests, especially in a CI environment. During local
                    // troubleshooting you may want to turn this off so you can see in the browser what's happening:
                    // Apart from changing the code here, you can use a configuration file or environment variables, see
                    // the docs.
                    configuration.BrowserConfiguration.Headless =
                        TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration:Headless", true);

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

                    // Enabling automatic HTML markup validation on every page change. With this, you can make sure that
                    // the HTML markup the app generates (also from content items) is valid. While the default settings
                    // for HTML validation are most possibly suitable for your projects, check out the
                    // HtmlValidationConfiguration class for what else you can configure. We've also added a
                    // .htmlvalidate.json file (note the Content Build Action) to further configure it.
                    configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = true;

                    // The UI Testing Toolbox can run several checks for the app even if you don't add explicit
                    // assertions: By default, the Orchard logs and the browser logs (where e.g. JavaScript errors show
                    // up) are checked and if there are any errors, the test will fail. You can also enable the checking
                    // of accessibility rules as we'll see later.
                    // Maybe not all of the default checks are suitable for you. Then it's simple to override them; here
                    // we change which log entries cause the tests to fail. We use the trick of making expected error
                    // messages not look like real errors.
                    configuration.AssertAppLogs = async webApplicationInstance =>
                        (await webApplicationInstance.GetLogOutputAsync())
                        .ReplaceOrdinalIgnoreCase(
                            "|Lombiq.TrainingDemo.Services.DemoBackgroundTask|ERROR|Expected non-error",
                            "|Lombiq.TrainingDemo.Services.DemoBackgroundTask|EXPECTED_ERROR|Expected non-error")
                        .ShouldNotContain("|ERROR|");

                    // You can adjust how HTML validation assertion works too.
                    configuration.HtmlValidationConfiguration.AssertHtmlValidationResult =
                        async validationResult =>
                        {
                            var errors = await validationResult.GetErrorsAsync();
                            errors.ShouldNotContain(error =>
                                // This is due to TheTheme. Only necessary until we get this fix:
                                // https://github.com/OrchardCMS/OrchardCore/pull/10292
                                !error.ContainsOrdinalIgnoreCase("Redundant role \"main\" on <main> (no-redundant-role)"));
                        };

                    changeConfiguration?.Invoke(configuration);
                });
    }
}

// NEXT STATION: Phew, that was a lot of configuration (and we have just scratched the surface!). Let's see some actual
// tests: Go to Tests/BasicTests.cs.
