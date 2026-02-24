# SQL Query Monitoring

SQL query monitoring captures database activity during Orchard Core requests and lets UI tests enforce performance-oriented expectations.

It detects:

- Duplicate command text (typical SELECT N+1 signal).
- Duplicate command text with identical parameters (typical missing-cache signal).
- Oversized result sets (typical missing SQL filter/paging signal).

## Quick Start

1. Enable SQL monitoring collection in test configuration.
2. Navigate to a page.
3. Assert the captured SQL summary.

```csharp
configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;
```

```csharp
await context.GoToRelativeUrlAsync("/categories/travel");
await context.AssertSqlQueryMonitoringAsync();
```

Use these assertion methods based on request flow:

- `AssertSqlQueryMonitoringAsync()`: Standard single-request page assertion.
- `AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync()`: Includes immediate follow-up async requests.
- `AssertSqlQueryMonitoringForRequestAsync(path, method)`: Match a specific request path/method explicitly.

## Debugging Failed Checks

When a check fails:

1. Read the failure category:
   - `DuplicateCommandText`: `Command text executed ...`
   - `DuplicateCommandWithParameters`: `Command text with same parameters executed ...`
   - `ResultSetRowCount`: `Command result set had ...`
2. Use the included SQL execution call stack(s) to locate the call site.
3. Review SQL counters emitted in test output:
   - Executions count
   - Duplicate command groups
   - Duplicate command+parameter groups
   - Largest observed row count
   - Triggered failure categories (at current thresholds)
4. Decide whether the issue is a real regression or a threshold/filter tuning need.

## Why Middleware Is Required

SQL monitoring is request-scoped. Middleware is needed to:

- Ensure SQL monitoring wrappers are active for the current request.
- Finalize request-level summaries at end of pipeline with: `RequestPath`, `RequestMethod`, `TraceIdentifier`, completion time, and executions.

Without middleware, assertions cannot reliably separate SQL activity per request.

## Configuration

Defaults:

- `EnableSqlQueryMonitoringCollection`: `false`
- `RunSqlQueryMonitoringAssertionOnAllPageChanges`: `false`
- `DuplicateCommandThreshold`: `30`
- `DuplicateCommandWithParametersThreshold`: `15`
- `ResultSetRowCountThreshold`: `200`
- `SummaryLookupTimeout`: `00:00:02`
- `SummaryLookupInterval`: `00:00:00.100`
- `FollowUpSummaryQuietPeriod`: `00:00:00.300`

How to think about these settings:

- `DuplicateCommandThreshold` is a broad duplicate-query guard. Lower it when you want earlier N+1 detection.
- `DuplicateCommandWithParametersThreshold` is stricter for exact repeat calls. Lower it when cache-related regressions are important.
- `ResultSetRowCountThreshold` guards unbounded reads. Lower it on list endpoints that should always be paged/filtered.
- `SummaryLookupTimeout` and `SummaryLookupInterval` control summary lookup stability right after navigation.
- `FollowUpSummaryQuietPeriod` controls how long follow-up-inclusive assertions keep waiting for late async requests.

Recommended tuning approach:

1. Start with defaults and run tests on stable pages.
2. Check emitted counters to understand normal query shape.
3. Lower thresholds gradually until they catch real regressions without frequent false positives.
4. Use per-page thresholds for known heavy pages instead of globally weakening checks.

Typical override:

```csharp
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 20;
configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 10;
configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 150;
```

Timing override:

```csharp
configuration.SqlQueryMonitoringConfiguration.SummaryLookupTimeout = TimeSpan.FromSeconds(5);
configuration.SqlQueryMonitoringConfiguration.SummaryLookupInterval = TimeSpan.FromMilliseconds(200);
configuration.SqlQueryMonitoringConfiguration.FollowUpSummaryQuietPeriod = TimeSpan.FromMilliseconds(500);
```

Automatic page-change assertions:

```csharp
configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;
configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
```

When enabled, page-change hooks use follow-up-inclusive assertions by default.

## Filtering Known Noisy Queries

Use filtering when a query pattern is expected, stable, and not actionable for the test goal. Typical examples are framework warm-up queries or metadata lookups that are known and benign in your environment.

Filtering is applied before thresholds are evaluated. This means filtered executions do not contribute to duplicate or row-count failures.

Good filtering practices:

- Match the narrowest stable command pattern possible.
- Prefer a short allowlist of known benign patterns over broad wildcard patterns.
- Revisit filters periodically; stale filters can hide real regressions.

Avoid:

- Filtering broad tables or generic `SELECT` patterns without additional constraints.
- Using filters to silence unstable tests that should be fixed by better setup or better thresholds.

```csharp
configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
        @"FROM\s+\[Document\].*ContentDefinitionRecord",
        @"FROM\s+\[Document\].*RolesDocument");
```

## Per-Page Thresholds

Use per-page thresholds when different routes legitimately have different SQL profiles. This keeps global checks strict while giving targeted flexibility for heavy but valid pages.

How matching works:

- Rules are evaluated against the request path.
- The first matching regex wins.
- If no rule matches, default thresholds are used.

Practical guidance:

- Keep defaults relatively strict.
- Add a specific rule for heavy routes (for example category listing pages).
- Keep regex patterns explicit and anchored (`^...$` or `^.../`) to avoid accidental matches.

```csharp
configuration.ConfigureSqlQueryMonitoringThresholdsForPages(
    new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(30, 15, 200),
    (Pattern: @"^/categories/.*", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(20, 10, 100)),
    (Pattern: @"^/about$", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(25, 12, 150)));
```

## Scenario Catalog

For complete, runnable scenarios, see [SQL query monitoring scenario catalog.](../../../Lombiq.OSOCE.Tests.UI/Tests/SqlMonitoringTests/README.md)
