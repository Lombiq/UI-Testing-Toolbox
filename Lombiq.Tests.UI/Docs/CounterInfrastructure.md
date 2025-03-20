# Counter Infrastructure

The counter infrastructure provides interfaces and essential classes to implement monitoring functionality for various subsystems, enabling the application under test to be observed from multiple perspectives.

## Monitoring Database Queries

This component helps identify the N+1 query problem in your application.

Its primary objective is to detect unexpected additional SQL queries at an early stage of development, ensuring performance remains under control throughout the application's lifecycle.

This part of the infrastructure injects a mocked implementation that replaces the original persistence services (`IStore` and `ISession`), making operations monitorable.

### Entrypoints Where the Counter Operates

- **Executing SQL Commands:** The counter tracks storage and retrieval operations performed by the persistence subsystem on the configured database. In short, every CRUD operation is counted. The implementation supports counting the same command both with and without comparing the parameter set.
  - _Example:_ The command `SELECT * FROM SampleTable WHERE Id=@d` is counted each time the command text is executed and separately for each unique parameter set.
- **Reading a Row from a Result Set:** Each row read from a result set is also counted.

## Configuration

You can configure thresholds for key persistence layer metrics across three different lifecycle stages.

### Entrypoints

Threshold values are stored in the configuration class: [`CounterThresholdConfiguration`](../Services/Counters/Configuration/CounterThresholdConfiguration.cs).

- `DbCommandIncludingParametersExecutionCountThreshold` – Compared against the internal counter, which increments each time a specific SQL command is executed with the same parameter set.
- `DbCommandExcludingParametersExecutionThreshold` – Compared against the internal counter, which increments each time a specific SQL command is executed, regardless of the parameter set.
- `DbReaderReadThreshold` – Compared against the internal counter, which increments each time a read operation is performed on a SQL result set.

### Lifecycles Where You Can Configure Thresholds

Threshold values are configured in: [`CounterConfiguration`](../Services/Counters/Configuration/CounterConfiguration.cs).

- **Navigation:** Captures operations performed during page load, including SQL queries executed for page generation and content retrieval (e.g., images from the database). The configuration is stored in `NavigationThreshold`.
- **Page Load:** Captures operations performed strictly during page load. The configuration is stored in `PageLoadThreshold`.
- **Session:** Captures operations executed while a given `ISession` instance is alive (e.g., a session used by a background task). The configuration is stored in `SessionThreshold`.

### Examples

You can find ready-to-run examples in [`DuplicatedSqlQueryDetectorTests`](../../Lombiq.Tests.UI.Samples/Tests/DuplicatedSqlQueryDetectorTests.cs).
