namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterThresholdConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the current threshold configuration and checking is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the threshold for the count of <see cref="System.Data.Common.DbCommand"/> executions, with the
    /// query only counted as a duplicate if both its text (<see cref="System.Data.Common.DbCommand.CommandText"/>) and
    /// parameters (<see cref="System.Data.Common.DbCommand.Parameters"/>) match. See
    /// <see cref="DbCommandExcludingParametersExecutionThreshold"/> for counting using only the command text.
    /// </summary>
    public int DbCommandIncludingParametersExecutionCountThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets the threshold for the count of <see cref="System.Data.Common.DbCommand"/> executions, with the
    /// query counted as a duplicate if its text (<see cref="System.Data.Common.DbCommand.CommandText"/>) matches.
    /// Parameters (<see cref="System.Data.Common.DbCommand.Parameters"/>) are not taken into account. See
    /// <see cref="DbCommandIncludingParametersExecutionCountThreshold"/> for counting using also the parameters.
    /// </summary>
    public int DbCommandExcludingParametersExecutionThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets the threshold of readings of <see cref="System.Data.Common.DbDataReader"/>s. Uses
    /// <see cref="System.Data.Common.DbCommand.CommandText"/> and <see cref="System.Data.Common.DbCommand.Parameters"/>
    /// for counting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to set the maximum number of reads allowed on a <see cref="System.Data.Common.DbDataReader"/> instace.
    /// The counter infrastructure counts the <see cref="System.Data.Common.DbDataReader.Read"/> and
    /// <see cref="System.Data.Common.DbDataReader.ReadAsync()"/> calls, also the
    /// <see cref="System.Collections.IEnumerator.MoveNext"/> calls are counted on
    /// <see cref="System.Collections.IEnumerator"/> instance returned by the
    /// <see cref="System.Data.Common.DbDataReader.GetEnumerator"/>.
    /// </para>
    /// </remarks>
    public int DbReaderReadThreshold { get; set; }
}
