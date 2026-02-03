using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// It's useful to demonstrate how SQL monitoring failures get surfaced. Here we use low thresholds and assert that the
// monitoring throws, so the test itself still passes.
public class SqlQueryMonitoringFailureTests : UITestBase
{
    public SqlQueryMonitoringFailureTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringShouldSurfaceIssues() =>
        ExecuteTestAfterSetupAsync(
            context => Should.ThrowAsync<SqlQueryMonitoringAssertionException>(() => context.AssertSqlQueryMonitoringAsync()),
            configuration =>
            {
                // We'll assert explicitly so the automatic on-page-change assertion doesn't consume the summary.
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = false;

                // Set deliberately low thresholds to trigger failures.
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 1;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 1;
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 1;

                return Task.CompletedTask;
            });
}

// NEXT STATION: Head over to Tests/SqlQueryMonitoringTenantTests.cs.
