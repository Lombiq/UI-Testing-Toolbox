using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Helpers;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples;

// This will be the base class for our UI test classes. Here we'll set up some common configuration. Inheriting test
// classes is not mandatory but the approach is simple and effective.
public abstract class UITestBase : OrchardCoreUITestBase<Program>
{
    protected UITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Two usual overloads. Note that we reference SetupHelpers.RunSetup as the setup operation: That's what will be run
    // when the first test executes. Until the setup is done, all other tests wait; then, they'll use the snapshot
    // created from the setup.
    // Do you use Auto Setup? No problem: Check out SetupHelpers.RunAutoSetup().
    // NEXT STATION: Check out SetupHelpers, then come back here!
    protected override Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, browser, SetupHelpers.RunSetupAsync, changeConfigurationAsync);

    // You could wrap all your tests by providing a different delegate as the first parameter of ExecuteTestAsync() and
    // do something before or after they're executed but this is not always necessary.
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
                // You should always set the window size of the browser, otherwise the size will be random based on the
                // settings of the given machine. However this is already handled as long as the
                // context.Configuration.BrowserConfiguration.DefaultBrowserSize option is properly set. You can change
                // it here but usually the default full HD is suitable.
                configuration.BrowserConfiguration.DefaultBrowserSize = CommonDisplayResolutions.HdPlus;

                // In headless mode, the browser's UI is not showing, it just runs in the background. This is what you
                // want to use when running all tests, especially in a CI environment. During local troubleshooting you
                // may want to turn this off so you can see in the browser what's happening. Hence the override of the
                // default here. Apart from changing the code here, you can use a configuration file or environment
                // variables, see the docs.
                configuration.BrowserConfiguration.Headless =
                    TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration:Headless", defaultValue: false);

                // There are event handlers that you can hook into, like
                // configuration.OrchardCoreConfiguration.BeforeAppStart. But it is just one of many. Check out the
                // others in OrchardCoreConfiguration if you're interested.

                // This is quite handy! We can, for example, add a configuration parameter when launching the app. This
                // can be used to set configuration for configuration providers, see the docs:
                // https://docs.orchardcore.net/en/latest/docs/reference/core/Configuration/. We can set e.g. Orchard's
                // AdminUrlPrefix like below, but this is just setting the default, so it's only an example. A more
                // useful example is enabling offline operation of the Lombiq Hosting - Azure Application Insights for
                // Orchard Core module (see the UI test project in
                // https://github.com/Lombiq/Orchard-Azure-Application-Insights).
                configuration.OrchardCoreConfiguration.BeforeAppStart +=
                    (_, argumentsBuilder) =>
                    {
                        argumentsBuilder
                            .AddWithValue("OrchardCore:OrchardCore_Admin:AdminUrlPrefix", "Admin");

                        return Task.CompletedTask;
                    };

                // Note that automatic HTML markup validation is enabled on every page change by default (you can
                // disable it with the below config). With this, you can make sure that the HTML markup the app
                // generates (also from content items) is valid. While the default settings for HTML validation are most
                // possibly suitable for your projects, check out the HtmlValidationConfiguration class for what else
                // you can configure. We've also added a custom .htmlvalidate.json file (note the Content Build
                // Action) to further configure it.
                ////configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;

                // For locally running apps, the UI Testing Toolbox configures Fake Logging (see
                // https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.testing). This provides an
                // in-memory log for assertions, regardless of the logging framework your app uses otherwise. By
                // default, it'll only collect log entries with the Error level and up. However, you can change this,
                // and the usual Logging app settings still work.
                configuration.OrchardCoreConfiguration.BeforeAppStart +=
                    (_, argumentsBuilder) =>
                    {
                        // This is how you can configure logging to be on the >=Error level in general, while still
                        // allowing info-level entries for ShellHost.
                        argumentsBuilder
                            .AddWithValue("Logging:LogLevel:Default", "Error")
                            .AddWithValue("Logging:LogLevel:System", "Error")
                            .AddWithValue("Logging:LogLevel:Microsoft", "Error")
                            .AddWithValue("Logging:LogLevel:OrchardCore.Environment.Shell.ShellHost", "Information");

                        return Task.CompletedTask;
                    };

                // Enabling FakeLogger to collect the info-level ShellHost log entries configured above. These will show
                // up in the test's output like "[Information] OrchardCore.Environment.Shell.ShellHost: Start creation
                // of shells".
                configuration.OrchardCoreConfiguration.AfterFakeLoggingConfiguration =
                    (_, fakeLogCollectorOptions) => fakeLogCollectorOptions.FilteredLevels.Add(LogLevel.Information);

                // Logging is important because the UI Testing Toolbox can run several checks for the app even if you
                // don't add explicit assertions: By default, the Orchard logs and the browser logs (where e.g.
                // JavaScript errors show up) are checked and if there are any errors, the test will fail. You can also
                // enable the checking of accessibility rules as we'll see later. Maybe not all of the default checks
                // are suitable for you. Then it's simple to override them; here we change which log entries cause the
                // tests to fail, and allow certain log entries: info-level log entries for ShellHost are allowed, see
                // above.
                configuration.AssertAppLogsAsync = webApplicationInstance =>
                    webApplicationInstance.LogsShouldNotContainAsync(
                        logEntry => logEntry.Message != "My permitted message." && logEntry.Level != LogLevel.Information,
                        configuration.TestCancellationToken);

                // Strictly speaking this is not necessary here, because we always use the same static method for setup.
                // However, if you used a dynamic setup operation (e.g. `context => SetupHelpers.RunSetupAsync(context,
                // someOtherVariable)` then the default setup identifier calculator would always return a new random
                // value, because it uses `setupOperation.GetHashCode()` under the hood. A custom calculator would fix
                // that. But in this example we just safely replace it with a human-readable name so the setup snapshot
                // directory is easier to find.
                configuration.SetupConfiguration.SetupOperationIdentifierCalculator = setupOperation =>
                    setupOperation == SetupHelpers.RunSetupAsync
                        ? "Sample Setup"
                        : OrchardCoreSetupConfiguration.DefaultSetupOperationIdentifierCalculator(setupOperation);

                if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);
            });
}

// NEXT STATION: Phew, that was a lot of configuration (and we have just scratched the surface!). Let's see some actual
// tests: Go to Tests/BasicTests.cs.
