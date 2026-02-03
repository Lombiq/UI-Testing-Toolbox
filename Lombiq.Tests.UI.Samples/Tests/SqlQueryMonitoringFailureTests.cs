using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using System;
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
            async context =>
            {
                try
                {
                    await context.AssertSqlQueryMonitoringAsync();
                    throw new InvalidOperationException("The SQL monitoring assertion did not fail as expected.");
                }
                catch (SqlQueryMonitoringAssertionException)
                {
                    _testOutputHelper.WriteLineTimestampedAndDebug(
                        "Caught SqlQueryMonitoringAssertionException as expected for the failure demo.");
                }
            },
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
