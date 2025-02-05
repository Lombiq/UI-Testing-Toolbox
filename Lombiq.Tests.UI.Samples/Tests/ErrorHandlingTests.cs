using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Samples.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Log;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Sometimes errors are expected. Let's check out what can be done with them!
public class ErrorHandlingTests : UITestBase
{
    public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // It's easier to diagnose a test failure if you know whether an element is missing because there something is
    // actually missing or there was a server-side error. The below test visits a page where the action method throws an
    // exception.
    [Fact]
    public Task ServerSideErrorOnLoadedPageShouldHaltTest() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                try
                {
                    await context.GoToErrorPageDirectlyAsync();

                    // This point should be unreachable because Orchard logs are automatically asserted after a page
                    // load.
                    throw new InvalidOperationException("The log assertion didn't happen after page load!");
                }
                catch (PageChangeAssertionException)
                {
                    // Remove all logs to have a clean slate.
                    await context.ClearLogsAsync();
                }
            });

    // You can interact with the browser log and its history as well. E.g. 404s and JS exceptions show up in the browser
    // log.
    [Fact]
    public Task ClientSideErrorOnLoadedPageShouldHaltTest() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                try
                {
                    await context.GoToRelativeUrlAsync("/this-does-not-exist");

                    // This point should be unreachable because browser logs are automatically asserted after a page
                    // load.
                    throw new InvalidOperationException("The log assertion didn't happen after page load!");
                }
                catch (PageChangeAssertionException)
                {
                    // Remove response logs to have a clean slate.
                    context.ClearCumulativeResponseLog();
                }
            });

    // To be able to trust the test above, we have to be sure that the browser logs survive the navigation events and
    // all get collected into the historic browser log.
    [Fact]
    public Task BrowserLogsShouldPersist() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                const string testLog = "--test log--";
                void WriteConsoleLog() => context.ExecuteScript($"console.info('{testLog}');");

                await context.SignInDirectlyAndGoToHomepageAsync();

                WriteConsoleLog();
                WriteConsoleLog();

                await context.GoToDashboardAsync();

                WriteConsoleLog();

                await context.GoToHomePageAsync();

                WriteConsoleLog();
                WriteConsoleLog();
                WriteConsoleLog();

                // Since the browser log is updated asynchronously, we have to wait for most recent entries to appear.
                ReliabilityHelper.DoWithRetriesOrFail(() =>
                    context
                    .CumulativeBrowserLog
                    .Count(entry => entry.Text.Contains(testLog)) == 6);
            },
            configuration =>
            {
                // By default, anything below warning is not logged to the browser log. So, to allow the info messages
                // of the test, we change the filter.
                configuration.BrowserLogFilter = logEntry =>
                    OrchardCoreUITestExecutorConfiguration.IsNonSuccessBrowserLogEntry(logEntry) || logEntry.Level == Level.Info;

                // By default, the test will fail if the browser log is not empty. We allow info entries here.
                configuration.AssertBrowserLog = logEntries => logEntries.ShouldNotContain(entry => entry.Level > Level.Info);
            });

    [Fact]
    public Task ErrorDuringSetupShouldHaltTest() =>
        Should.ThrowAsync<PageChangeAssertionException>(() =>
            ExecuteTestAfterSetupAsync(
                _ => throw new InvalidOperationException("This point shouldn't be reachable because setup fails."),
                configuration =>
                {
                    // The test is guaranteed to fail so we don't want to retry it needlessly.
                    configuration.MaxRetryCount = 0;

                    // Otherwise, a GitHub Actions error annotation would appear in the workflow run summary, indicating
                    // a problem, despite them being expected.
                    configuration.GitHubActionsOutputConfiguration.EnableErrorAnnotations = false;

                    // We introduce a custom setup operation that has an intentionally invalid SQL Server configuration.
                    configuration.SetupConfiguration.SetupOperation = async context =>
                    {
                        await context.GoToSetupPageAndSetupOrchardCoreAsync(
                            new OrchardCoreSetupParameters(context)
                            {
                                SiteName = "Setup Error Test",
                                RecipeId = SetupHelpers.RecipeId,
                                DatabaseProvider = OrchardCoreSetupPage.DatabaseType.SqlServer,
                                ConnectionString = "An invalid connection string which causes an error during setup.",
                            });

                        throw new InvalidOperationException(
                            "This point shouldn't be reachable if the logs are properly kept.");
                    };

                    // No need to create a failure dump folder for this test, since it'll always fail.
                    configuration.TestDumpConfiguration.CreateTestDump = false;
                }));
}

// END OF TRAINING SECTION: Error handling.
// NEXT STATION: Head over to Tests/MonkeyTests.cs.
