# SQL Query Monitoring

The UI Testing Toolbox can monitor SQL commands executed during a request and help you detect potentially inefficient
data access patterns. This is useful for catching:

- **SELECT N+1 queries**: The same SQL command text executed repeatedly with different parameters.
- **Cacheable duplicates**: The same SQL command text executed repeatedly with identical parameters.
- **Oversized result sets**: Queries returning more rows than expected, which might indicate missing filters.

The monitoring runs inside the Orchard Core app under test. Assertions and configuration live in your UI test project,
just like HTML validation and accessibility checking.

## How it works

When UI testing is enabled, the app tracks SQL commands executed during each request. The test project then pulls the
summary of the most recent request with SQL activity and applies configurable assertions.

Monitoring summaries are stored per request, so typically you should assert after each navigation (or after the action
that should be checked).

Row counts are based on how many rows were actually read from the data reader. If a query isn't fully enumerated, the
recorded row count will be lower than the total result set size.

## Configuration

Configure thresholds on a per-test basis via `SqlQueryMonitoringConfiguration`:

```csharp
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 20;
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 10;
configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 200;
```

Threshold semantics:

- **DuplicateCommandThreshold**: Fails when the same SQL command text is executed **at least** this many times in a
  request, regardless of parameters.
- **DuplicateCommandWithParametersThreshold**: Fails when the same SQL command text **and** parameter values are
  executed **at least** this many times in a request.
- **ResultSetRowCountThreshold**: Fails when a command returns **more** rows than this value.

You can also enable automatic assertions on every page change:

```csharp
configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
```

## Filtering executions

If a test environment runs some expected warm-up queries, you can ignore them while keeping the default assertions:

```csharp
configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
        @"FROM\s+\[Document\].*ContentDefinitionRecord",
        @"FROM\s+\[Document\].*RolesDocument");
```

The filter runs before any thresholds are applied.

## Manual assertions

To assert explicitly (and optionally provide custom logic), use the `AssertSqlQueryMonitoringAsync` extension:

```csharp
await context.AssertSqlQueryMonitoringAsync();

await context.AssertSqlQueryMonitoringAsync(summary =>
{
    summary.Executions.ShouldNotBeEmpty();
    return Task.CompletedTask;
});
```

If you want the summary itself for custom reporting, you can use:

```csharp
var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
```

## Sample

See the sample test for a fully documented example:

- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringBasicsTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringFailureTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringTenantTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringThresholdsTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringPageChangeRuleTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringFilteringTests.cs`
