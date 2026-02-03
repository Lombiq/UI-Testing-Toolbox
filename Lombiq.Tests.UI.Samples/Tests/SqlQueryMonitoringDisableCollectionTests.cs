using Lombiq.Tests.UI.Extensions;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// SQL monitoring can be disabled when you only want the UI test helpers without the collection overhead.
public class SqlQueryMonitoringDisableCollectionTests : UITestBase
{
    public SqlQueryMonitoringDisableCollectionTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringShouldAllowDisablingCollection() =>
        ExecuteTestAfterSetupAsync(
            context => context.GoToHomePageAsync(onlyIfNotAlreadyThere: false),
            configuration =>
            {
                // Disable SQL query monitoring collection entirely.
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = false;
                return Task.CompletedTask;
            });
}

// NEXT STATION: Head over to Tests/SqlQueryMonitoringFailureTests.cs.
