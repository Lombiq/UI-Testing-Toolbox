using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.Counters.Configuration;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Some times you may want to detect duplicated SQL queries. This can be useful if you want to make sure that your code
// does not execute the same query multiple times.
public class DuplicatedSqlQueryDetectorTests : UITestBase
{
    public DuplicatedSqlQueryDetectorTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // This test will fail because the app will read the the same command result more times then the configured threshold
    // during the Admin page rendering.
    [Theory, Chrome]
    public Task DbReaderReadDuringRunningPhaseShouldThrow(Browser browser) =>
        Should.ThrowAsync<AggregateException>(() =>
            ExecuteTestAfterSetupAsync(
                context => context.SignInDirectlyAndGoToDashboardAsync(),
                browser,
                ConfigureAsync));

    // This test will pass because not the Admin page was loaded.
    [Theory, Chrome]
    public Task DbReaderReadDuringRunningPhaseShouldNotThrow(Browser browser) =>
        Should.NotThrowAsync(() =>
            ExecuteTestAfterSetupAsync(
                async context => await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false),
                browser,
                ConfigureAsync));

    // We configure the test to throw an exception if a certain counter threshold is exceeded, but only in case of Admin page.
    private static Task ConfigureAsync(OrchardCoreUITestExecutorConfiguration configuration)
    {
        // The test is guaranteed to fail so we don't want to retry it needlessly.
        configuration.MaxRetryCount = 0;

        var adminCounterConfiguration = new CounterConfiguration();
        // Let's enable and configure the counter threshold for ORM sessions.
        adminCounterConfiguration.SessionThreshold.Disable = false;
        adminCounterConfiguration.SessionThreshold.DbReaderReadThreshold = 0;
        // Apply the configuration to the Admin page only.
        configuration.CounterConfiguration.Running.Add(
            new RelativeUrlConfigurationKey(new Uri("/Admin", UriKind.Relative), exactMatch: false),
            adminCounterConfiguration);

        return Task.CompletedTask;
    }
}

// END OF TRAINING SECTION: Duplicated SQL query detector.
