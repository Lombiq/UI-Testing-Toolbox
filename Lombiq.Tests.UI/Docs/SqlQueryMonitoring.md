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
`DbCommand` for the current request scope.

The wrappers cover:

- Reader, scalar, and non-query command paths.
- Sync and async ADO.NET execution.
- Transaction paths as well.

This means monitoring covers normal YesSql usage and custom query paths that run through the wrapped connection.

After the request completes, the UI test project fetches a summary and applies assertions.

When asserting for the current page, the toolkit first tries to match a summary to the browser path (including query
string) with a short retry window to avoid timing races right after navigation. If no exact match is found, it falls
back to the most relevant recent summary.
`AssertSqlQueryMonitoringAsync()` asserts this latest matched summary only.

Row counts are based on how many rows were actually read from the data reader. Enumeration through `foreach` or LINQ
also counts rows. If a query is not fully enumerated, the recorded row count will be lower than the full result set
size.

Summaries are consumed when you assert, so each request should be asserted once.

## Configuration

Set thresholds per test with `SqlQueryMonitoringConfiguration` in the test configuration delegate passed to
`ExecuteTestAfterSetupAsync`. Defaults:

- `RunSqlQueryMonitoringAssertionOnAllPageChanges`: `false`
- `DuplicateCommandThreshold`: 30
- `DuplicateCommandWithParametersThreshold`: 15
- `ResultSetRowCountThreshold`: 200
- `SummaryLookupTimeout`: 2 seconds
- `SummaryLookupInterval`: 100 milliseconds
- `FollowUpSummaryQuietPeriod`: 300 milliseconds

Typical override:

```csharp
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 20;
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 10;
configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 200;
```

Timing override example:

```csharp
configuration.SqlQueryMonitoringConfiguration.SummaryLookupTimeout = TimeSpan.FromSeconds(5);
configuration.SqlQueryMonitoringConfiguration.SummaryLookupInterval = TimeSpan.FromMilliseconds(200);
configuration.SqlQueryMonitoringConfiguration.FollowUpSummaryQuietPeriod = TimeSpan.FromMilliseconds(500);
```

Threshold semantics:

- **DuplicateCommandThreshold**: Fails when the same SQL command text is executed at least this many times in a
  request, regardless of parameters.
- **DuplicateCommandWithParametersThreshold**: Fails when the same SQL command text and parameter values are executed
  at least this many times in a request.
- **ResultSetRowCountThreshold**: Fails when a command returns more rows than this value.

Automatic assertions on page change are opt-in:

```csharp
configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
```

`SqlQueryMonitoringAndAssertionOnPageChangeRule` is only evaluated when
`RunSqlQueryMonitoringAssertionOnAllPageChanges` is `true`.
When enabled, the built-in page-change hook uses follow-up-inclusive assertion to capture immediate async requests too.

## Enabling/Disable Collection

SQL query monitoring collection can be disabled for a test run. When disabled, the Orchard Core app does not register
the SQL monitoring services or middleware, so there is no monitoring overhead and no summaries to assert.

```csharp
configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = false;
```

## Filtering Executions

If a test environment runs expected warm-up queries, you can ignore them while keeping the default assertions:

```csharp
configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
        @"FROM\s+\[Document\].*ContentDefinitionRecord",
        @"FROM\s+\[Document\].*RolesDocument");
```

The filter runs before thresholds are applied. Patterns are matched with a 1-second regex timeout.

## Manual Assertions

Use these assertion methods based on what kind of request flow you're validating:

- `AssertSqlQueryMonitoringAsync()`:
  For normal page-change assertions where the latest matched summary should be asserted as a single request.
- `AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync()`:
  For aggressive assertions where follow-up async requests after page load should be aggregated too.
- `AssertSqlQueryMonitoringForRequestAsync(path, method)`:
  For request-specific assertions, including non-navigation requests.

Basic page-change assertion:

```csharp
await context.AssertSqlQueryMonitoringAsync();

await context.AssertSqlQueryMonitoringAsync(summary =>
{
    summary.Executions.ShouldNotBeEmpty();
    return Task.CompletedTask;
});
```

Combined assertion with follow-up async requests:

```csharp
await context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(summary =>
{
    summary.Executions.ShouldNotBeEmpty();
    return Task.CompletedTask;
});
```

If you want the summary itself for custom reporting:

```csharp
var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
```

If your test triggers API calls without browser navigation (for example with browser-side `fetch`), assert by request
path:

```csharp
await context.AssertSqlQueryMonitoringForRequestAsync(
    "/Lombiq.HelpfulLibraries.Samples/LinqToDbSamples/SimpleQuery",
    requestMethod: "GET");
```

For non-HTML endpoints (for example plain text sample actions), you can:

- Disable HTML validation for the test and navigate directly, then use `AssertSqlQueryMonitoringAsync()`.

## Per-Page Thresholds

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

The first matching pattern wins. Patterns are matched against the request path (for example `/categories/travel`).

## Test Output Counters

After each `AssertSqlQueryMonitoringAsync` call, test output includes a compact counters snapshot for diagnostics.

## Interpreting Failures

Use the failure category to guide your next step:

- **Duplicate command text**: Look for SELECT N+1 patterns. Typical fixes include batching queries or moving querying
  logic out of loops.
- **Duplicate command text with same parameters**: Look for missing caching or repeated calls from multiple code paths.
- **Oversized result sets**: Look for missing SQL filters, ordering, or paging.

The failure message lists the exact queries that crossed thresholds so you can navigate to the call site.
Each failure also includes the captured SQL execution call stack(s), untrimmed, to help identify where the command
originated.

## Samples

See the SQL monitoring scenario catalog in:

- `test/Lombiq.OSOCE.Tests.UI/Tests/SqlMonitoringTests/README.md`
