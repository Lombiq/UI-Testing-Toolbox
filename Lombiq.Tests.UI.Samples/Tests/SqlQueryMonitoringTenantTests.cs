using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// SQL monitoring is tenant-aware. This test creates a tenant, switches to it, and verifies monitoring still works.
public class SqlQueryMonitoringTenantTests : UITestBase
{
    public SqlQueryMonitoringTenantTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringShouldWorkOnAnotherTenant() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                const string tenantName = "SqlMonitorTest";
                const string tenantUrlPrefix = "sql-monitor-test";
                const string tenantDisplayName = "Lombiq's OSOCE - SQL Monitoring Tenant";
                const string tenantAdminName = "tenantSqlMonitorAdmin";

                await context.SignInDirectlyAsync();
                await context.CreateAndSwitchToTenantAsync(
                    tenantName,
                    tenantUrlPrefix,
                    new OrchardCoreSetupParameters
                    {
                        SiteName = tenantDisplayName,
                        RecipeId = "Lombiq.OSOCE.Tests",
                        TablePrefix = tenantUrlPrefix,
                        UserName = tenantAdminName,
                    });

                await context.GoToRelativeUrlAsync("/");

                context.GetCurrentUri().AbsolutePath.ShouldStartWith($"/{tenantUrlPrefix}");

                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
                    return Task.CompletedTask;
                });

                context.SwitchCurrentTenantToDefault();
                await context.GoToRelativeUrlAsync("/");

                context.GetCurrentUri().AbsolutePath.ShouldNotStartWith($"/{tenantUrlPrefix}");

                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
                    return Task.CompletedTask;
                });
            },
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = false;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 30;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 15;
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 200;
                return Task.CompletedTask;
            });
}

// NEXT STATION: Head over to Tests/SqlQueryMonitoringThresholdsTests.cs.
