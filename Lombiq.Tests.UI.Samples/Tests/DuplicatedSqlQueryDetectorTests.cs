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

public class DuplicatedSqlQueryDetectorTests : UITestBase
{
    public DuplicatedSqlQueryDetectorTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task RepeatedSqlQueryDuringRunningPhaseShouldThrow(Browser browser) =>
        Should.ThrowAsync<AggregateException>(() =>
            ExecuteTestAfterSetupAsync(
                context => context.SignInDirectlyAndGoToDashboardAsync(),
                browser,
                ConfigureAsync));

    [Theory, Chrome]
    public Task DbReaderReadDuringRunningPhaseShouldThrow(Browser browser) =>
        Should.ThrowAsync<AggregateException>(() =>
            ExecuteTestAfterSetupAsync(
                async context => await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false),
                browser,
                ConfigureAsync));

    private static Task ConfigureAsync(OrchardCoreUITestExecutorConfiguration configuration)
    {
        // The test is guaranteed to fail so we don't want to retry it needlessly.
        configuration.MaxRetryCount = 0;

        var adminCounterConfiguration = new CounterConfiguration();
        adminCounterConfiguration.SessionThreshold.Disable = false;
        adminCounterConfiguration.SessionThreshold.DbReaderReadThreshold = 0;
        configuration.CounterConfiguration.Running.Add(
            new RelativeUrlConfigurationKey(new Uri("/Admin", UriKind.Relative), exactMatch: false),
            adminCounterConfiguration);

        return Task.CompletedTask;
    }
}

// END OF TRAINING SECTION: Duplicated SQL query detector.
