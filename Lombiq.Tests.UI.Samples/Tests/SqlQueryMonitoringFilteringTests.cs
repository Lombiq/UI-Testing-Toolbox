using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// This test shows how to filter out known noisy queries while still using the default threshold assertions.
public class SqlQueryMonitoringFilteringTests : UITestBase
{
    public SqlQueryMonitoringFilteringTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringShouldAllowIgnoringKnownQueries() =>
        ExecuteTestAfterSetupAsync(
            context => context.AssertSqlQueryMonitoringAsync(),
            configuration =>
            {
                // Keep thresholds low to make filtering behavior visible, but still high enough for stable tests.
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 5;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 3;
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 5;

                // Ignore known Orchard Core warmup/document queries and index lookups to keep the sample stable.
                configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
                    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
                        @"FROM\s+\[Document\].*\[Type\]\s*=\s*@Type",
                        @"FROM\s+\[Document\].*ContentDefinitionRecord",
                        @"FROM\s+\[Document\].*RolesDocument",
                        @"FROM\s+\[Document\].*PlacementsDocument",
                        @"FROM\s+\[Document\].*LayersDocument",
                        @"FROM\s+\[Document\].*TemplatesDocument",
                        @"FROM\s+\[ContentItemIndex\]",
                        @"FROM\s+\[AutoroutePartIndex\]");
            });
}
