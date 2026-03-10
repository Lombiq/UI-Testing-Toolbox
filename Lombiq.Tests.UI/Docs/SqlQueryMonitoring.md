# SQL Query Monitoring

SQL query monitoring captures what SQL ran during an Orchard Core request, so your UI tests can check for common performance problems.

It detects:

- Duplicate command text (typical SELECT N+1 signal).
- Duplicate command text with identical parameters (typical missing-cache signal).
- Oversized result sets (typical missing SQL filter/paging signal).

## Basic Flow

1. Enable SQL monitoring collection for the test run.
2. Navigate to a page or hit an endpoint.
3. Assert the captured summary manually. Or turn on automatic page-change assertions and let the test fail if a summary exceeds thresholds on any page change.

See the complete sample in
[`test/Lombiq.UITestingToolbox/Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringTests.cs`](../../Lombiq.Tests.UI.Samples/Tests/SqlQueryMonitoringTests.cs).

Use these manual assertion methods:

- `AssertSqlQueryMonitoringAsync()`: Standard single-request page assertion.
- `AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync()`: Also includes immediate follow-up async requests from the same tenant.
- `AssertSqlQueryMonitoringForRequestAsync(requestPathOrUrl, requestMethod)`: Asserts a specific request path/query or absolute URL. `requestMethod` is optional.

The default automatic page-change assertions runs the `AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync()` method after every page change, so it includes follow-up async requests too.

## Debugging Failed Checks

When a check fails, first check the failure category:

- `DuplicateCommandText`
- `DuplicateCommandWithParameters`
- `ResultSetRowCount`

Then look at the SQL call stacks and the SQL counters in the test output. That usually tells you whether this is a real regression or just something to tune.

## Configuration

Defaults:

| Setting | Default | What it does |
| --- | --- | --- |
| `EnableSqlQueryMonitoringCollection` | `false` | Turns SQL monitoring on for the app under test. If this is `false`, nothing is collected. |
| `RunSqlQueryMonitoringAssertionOnAllPageChanges` | `false` | Runs SQL monitoring assertions automatically after every page change. |
| `DuplicateCommandThreshold` | `30` | Fails if the same SQL command text runs 30 times or more in one request. |
| `DuplicateCommandWithParametersThreshold` | `15` | Fails if the same SQL command text with the same parameters runs 15 times or more in one request. |
| `ResultSetRowCountThreshold` | `200` | Fails if a single SQL command returns more than 200 rows. |
| `SummaryLookupTimeout` | `00:00:02` | How long assertions wait for the expected summary to show up. |
| `SummaryLookupInterval` | `00:00:00.100` | How often assertions poll while waiting for a summary. |
| `FollowUpSummaryQuietPeriod` | `00:00:00.300` | How long follow-up assertions keep waiting after the last newly captured summary. |

## Scenario Catalog

For complete scenarios, see
[`test/Lombiq.OSOCE.Tests.UI/Tests/SqlMonitoringTests/Readme.md`](../../../Lombiq.OSOCE.Tests.UI/Tests/SqlMonitoringTests/Readme.md).
