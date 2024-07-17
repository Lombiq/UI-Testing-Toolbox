namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class PhaseCounterConfiguration : CounterConfiguration
{
    /// <summary>
    /// Gets or sets threshold configuration used under the app phase (setup, running) lifetime.
    /// </summary>
    public CounterThresholdConfiguration PhaseThreshold { get; set; } = new CounterThresholdConfiguration
    {
        DbCommandIncludingParametersExecutionCountThreshold = 22,
        DbCommandExcludingParametersExecutionThreshold = 44,
        DbReaderReadThreshold = 11,
    };
}
