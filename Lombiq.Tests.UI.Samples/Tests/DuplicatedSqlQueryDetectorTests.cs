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
// does not execute the same query multiple times, wasting time and computing resources.
public class DuplicatedSqlQueryDetectorTests : UITestBase
{
    public DuplicatedSqlQueryDetectorTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // This test will fail because the app will read the same command result more times than the configured threshold
    // during the Admin page rendering.
    [Fact]
    public Task PageWithTooManyDuplicatedSqlQueriesShouldThrow() =>
        Should.ThrowAsync<AggregateException>(() =>
            ExecuteTestAfterSetupAsync(
                context => context.SignInDirectlyAndGoToDashboardAsync(),
                configuration => ConfigureAsync(
                    configuration,
                    commandExcludingParametersThreshold: 3,
                    commandIncludingParametersThreshold: 2,
                    readerReadThreshold: 0)));

    // This test will pass because not any of the Admin page was loaded where the SQL queries are under monitoring.
    [Fact]
    public Task PageWithoutDuplicatedSqlQueriesShouldPass() =>
        ExecuteTestAfterSetupAsync(
            context => context.GoToHomePageAsync(onlyIfNotAlreadyThere: false),
            configuration => ConfigureAsync(configuration));

    // This test will pass because counter thresholds are exactly matching with the counter values captured during
    // navigating to the Admin dashboard page.
    [Fact]
    public Task PageWithMatchingCounterThresholdsShouldPass() =>
        ExecuteTestAfterSetupAsync(
            context => context.SignInDirectlyAndGoToDashboardAsync(),
            configuration => ConfigureAsync(
                configuration,
                commandExcludingParametersThreshold: 3,
                commandIncludingParametersThreshold: 2,
                readerReadThreshold: 2));

    // We configure the test to throw an exception if a certain counter threshold is exceeded, but only in case of Admin
    // pages.
    private static Task ConfigureAsync(
        OrchardCoreUITestExecutorConfiguration configuration,
        int commandExcludingParametersThreshold = 0,
        int commandIncludingParametersThreshold = 0,
        int readerReadThreshold = 0)
    {
        // The test is guaranteed to fail so we don't want to retry it needlessly.
        configuration.MaxRetryCount = 0;

        var adminCounterConfiguration = new CounterConfiguration
        {
            ExcludeFilter = OrchardCoreUITestExecutorConfiguration.DefaultCounterExcludeFilter,
            SessionThreshold =
            {
                // Let's enable and configure the counter thresholds for ORM sessions.
                IsEnabled = true,
                DbCommandExcludingParametersExecutionThreshold = commandExcludingParametersThreshold,
                DbCommandIncludingParametersExecutionCountThreshold = commandIncludingParametersThreshold,
                DbReaderReadThreshold = readerReadThreshold,
            },
        };

        // Apply the configuration to the Admin pages only.
        configuration.CounterConfiguration.AfterSetup.Add(
            new RelativeUrlConfigurationKey(new Uri("/Admin", UriKind.Relative), exactMatch: false),
            adminCounterConfiguration);

        // Enable the counter subsystem.
        configuration.CounterConfiguration.IsEnabled = true;

        return Task.CompletedTask;
    }
}

// END OF TRAINING SECTION: Duplicated SQL query detector.
