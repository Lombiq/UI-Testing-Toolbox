namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class PhaseCounterConfiguration : CounterConfiguration
{
    /// <summary>
    /// Gets or sets threshold configuration used under app phase(setup, running) lifetime.
    /// </summary>
    public CounterThresholdConfiguration PhaseThreshold { get; set; } = new CounterThresholdConfiguration
    {
        DbCommandExecutionThreshold = 22,
        DbCommandTextExecutionThreshold = 44,
        DbReaderReadThreshold = 11,
    };
}
