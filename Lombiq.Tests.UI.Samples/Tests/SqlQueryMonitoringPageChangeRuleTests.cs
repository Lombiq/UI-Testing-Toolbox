using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// You can also customize which page changes should be monitored by configuring a predicate.
public class SqlQueryMonitoringPageChangeRuleTests : UITestBase
{
    public SqlQueryMonitoringPageChangeRuleTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringShouldRespectPageChangeRule()
    {
        var summaries = new List<SqlQueryMonitoringSummary>();

        return ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.GoToRelativeUrlAsync("/about");

                summaries.Count.ShouldBe(1);
                summaries[0].RequestPath.ShouldContain("/categories");
                summaries[0].Executions.ShouldNotBeEmpty(
                    "SQL query monitoring should capture at least one command.");
            },
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.SqlQueryMonitoringAndAssertionOnPageChangeRule =
                    context => context.GetCurrentUri().AbsolutePath.Contains("/categories");
                configuration.SqlQueryMonitoringConfiguration.AssertSqlQueryMonitoringSummaryAsync = summary =>
                {
                    summaries.Add(summary);
                    return Task.CompletedTask;
                };
                return Task.CompletedTask;
            });
    }
}

// NEXT STATION: Head over to Tests/SqlQueryMonitoringFilteringTests.cs.
