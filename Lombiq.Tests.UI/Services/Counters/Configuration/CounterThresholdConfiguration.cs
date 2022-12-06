namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterThresholdConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the current threshold configuration and checking is disabled.
    /// </summary>
    public bool Disable { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold of executed <see cref="System.Data.Common.DbCommand"/>s. Uses
    /// <see cref="System.Data.Common.DbCommand.CommandText"/> and <see cref="System.Data.Common.DbCommand.Parameters"/>
    /// for counting.
    /// </summary>
    public int DbCommandExecutionThreshold { get; set; } = 11;

    /// <summary>
    /// Gets or sets the threshold of executed <see cref="System.Data.Common.DbCommand"/>s. Uses
    /// <see cref="System.Data.Common.DbCommand.CommandText"/> for counting.
    /// </summary>
    public int DbCommandTextExecutionThreshold { get; set; } = 11;

    /// <summary>
    /// Gets or sets the threshold of readings of <see cref="System.Data.Common.DbCommand"/>s. Uses
    /// <see cref="System.Data.Common.DbCommand.CommandText"/> and <see cref="System.Data.Common.DbCommand.Parameters"/>
    /// for counting.
    /// </summary>
    public int DbReaderReadThreshold { get; set; } = 11;
}
