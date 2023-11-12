namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterThresholdConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the current threshold configuration and checking is disabled.
    /// </summary>
    public bool Disable { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold for the count of <see cref="System.Data.Common.DbCommand"/> executions, with the
    /// query only counted as a duplicate if both its text (<see cref="System.Data.Common.DbCommand.CommandText"/>) and
    /// parameters (<see cref="System.Data.Common.DbCommand.Parameters"/>) match. See
    /// <see cref="DbCommandTextExecutionThreshold"/> for counting using only the command text.
    /// </summary>
    public int DbCommandIncludingParametersExecutionCountThreshold { get; set; } = 11;

    /// <summary>
    /// Gets or sets the threshold for the count of <see cref="System.Data.Common.DbCommand"/> executions, with the
    /// query counted as a duplicate if its text (<see cref="System.Data.Common.DbCommand.CommandText"/>) matches.
    /// Parameters (<see cref="System.Data.Common.DbCommand.Parameters"/>) are not taken into account. See
    /// <see cref="DbCommandExecutionThreshold"/> for counting using also the parameters.
    /// </summary>
    public int DbCommandExcludingParametersExecutionThreshold { get; set; } = 11;

    /// <summary>
    /// Gets or sets the threshold of readings of <see cref="System.Data.Common.DbCommand"/>s. Uses
    /// <see cref="System.Data.Common.DbCommand.CommandText"/> and <see cref="System.Data.Common.DbCommand.Parameters"/>
    /// for counting.
    /// </summary>
    public int DbReaderReadThreshold { get; set; } = 11;
}
