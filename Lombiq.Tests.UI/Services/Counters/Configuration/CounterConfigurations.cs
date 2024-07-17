namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterConfigurations
{
    /// <summary>
    /// Gets or sets a value indicating whether the whole counter infrastructure is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the counter configuration used in the setup phase of the web application.
    /// </summary>
    public PhaseCounterConfiguration Setup { get; } = new();

    /// <summary>
    /// Gets the counter configuration used in the running phase of the web application.
    /// </summary>
    public RunningPhaseCounterConfiguration AfterSetup { get; } = [];
}
