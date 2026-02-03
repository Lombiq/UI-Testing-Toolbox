# SQL Query Monitoring

SQL query monitoring captures database activity during Orchard Core requests and lets your UI tests enforce
performance‑oriented expectations. It helps you catch:

- **SELECT N+1 queries**: The same SQL command text executed repeatedly with different parameters.
- **Cacheable duplicates**: The same SQL command text executed repeatedly with identical parameters.
- **Oversized result sets**: Queries returning more rows than expected, which might indicate missing filters.

The monitoring runs inside the Orchard Core app under test. Assertions and configuration live in your UI test project,
just like HTML validation and accessibility checking.

## How it works

When UI testing is enabled, the Orchard Core app wraps the YesSql connection factory and records every executed
`DbCommand` for the current request scope. After the request completes, the UI test project fetches the most recent
summary and applies the configured assertions.

Monitoring summaries are stored per request, so typically you should assert after each navigation (or after the action
that should be checked).

Row counts are based on how many rows were actually read from the data reader. Enumeration through `foreach` or LINQ
also counts rows. If a query is not fully enumerated, the recorded row count will be lower than the total result set
size. The summaries are stored per request and consumed when you assert, so each request should be asserted once.

## Configuration

Set thresholds per test with `SqlQueryMonitoringConfiguration` in the test configuration delegate passed to
`ExecuteTestAfterSetupAsync`. Baseline defaults are provided and can be tuned per feature or per page. Defaults:

- `DuplicateCommandThreshold`: 30
- `DuplicateCommandWithParametersThreshold`: 15
- `ResultSetRowCountThreshold`: 200

Adjust thresholds when a page has known query volume (e.g., lists, dashboards) or when you want to tighten limits for a
specific feature. The snippet below shows a typical override.

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

You can also enable automatic assertions on every page change (enabled by default):

```csharp
configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
```

## Enabling/disable collection

SQL query monitoring collection can be disabled for a test run. When disabled, the Orchard Core app does not register
the SQL monitoring services or middleware, so there is no overhead and no summaries to assert.

```csharp
configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = false;
```

## Filtering executions

If a test environment runs some expected warm-up queries, you can ignore them while keeping the default assertions:

```csharp
configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
        @"FROM\s+\[Document\].*ContentDefinitionRecord",
        @"FROM\s+\[Document\].*RolesDocument");
```

The filter runs before any thresholds are applied, and patterns are matched with a 1-second regex timeout to avoid
pathological patterns.

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

## Per-page thresholds

You can change thresholds based on the target URL using regex rules:

```csharp
configuration.ConfigureSqlQueryMonitoringThresholdsForPages(
    new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
        DuplicateCommandThreshold: 30,
        DuplicateCommandWithParametersThreshold: 15,
        ResultSetRowCountThreshold: 200),
    (Pattern: @"^/categories/.*", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
        DuplicateCommandThreshold: 20,
        DuplicateCommandWithParametersThreshold: 10,
        ResultSetRowCountThreshold: 100)),
    (Pattern: @"^/about$", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
        DuplicateCommandThreshold: 25,
        DuplicateCommandWithParametersThreshold: 12,
        ResultSetRowCountThreshold: 150)));
```

The first matching pattern wins. Patterns are matched against the request path (e.g. `/categories/travel`).

## Test output counters

After each `AssertSqlQueryMonitoringAsync` call, the test output includes a compact counters snapshot to help with
diagnostics. Example:

```
SQL monitoring counters after page change:
- Request: GET /categories/travel
- Executions: 42
- Duplicate command groups: 3 (max group size: 10, threshold: 30)
- Duplicate command+parameters groups: 2 (max group size: 6, threshold: 15)
- Result set rows observed: 5 (max rows: 120, threshold: 200)
```

Explanation:

- **Executions**: Total SQL commands recorded for the request after filtering.
- **Duplicate command groups**: How many distinct command texts were repeated. “Max group size” is the highest repeat
  count for any single command text, compared to the configured threshold.
- **Duplicate command+parameters groups**: Same as above but includes parameter values. This is useful for spotting
  missed caching.
- **Result set rows observed**: How many commands produced rows based on actual enumeration. “Max rows” is the largest
  observed row count for a single command, compared to the threshold.

## Interpreting failures

Use the failure category to guide your next step:

- **Duplicate command text**: Look for SELECT N+1 patterns. In .NET Core code, common fixes include batching with
  a single YesSql query or refactoring loops to query once.
- **Duplicate command text with same parameters**: Look for missing caching or repeated calls from multiple sites.
  In .NET Core, common fixes include using `IMemoryCache` or `IDistributedCache` or extracting shared query logic behind a service.
- **Oversized result sets**: Look for missing SQL filters or ordering. In .NET Core, common fixes include adding
  query filters or paging at the database level before projecting results.

The failure message lists the exact queries that crossed the threshold so you can navigate to the call site.

## Sample

See the sample tests for a fully documented example:

- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringBasicsTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringDisableCollectionTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringFailureTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringTenantTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringThresholdsTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringPageChangeRuleTests.cs`
- `Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringFilteringTests.cs`
