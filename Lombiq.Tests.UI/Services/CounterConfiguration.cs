using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services.Counters;
using Lombiq.Tests.UI.Services.Counters.Data;
using Lombiq.Tests.UI.Services.Counters.Value;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services;

public class CounterConfiguration
{
    /// <summary>
    /// Gets the counter configuration used in the setup phase.
    /// </summary>
    public PhaseCounterConfiguration Setup { get; } = new();

    /// <summary>
    /// Gets the counter configuration used in the running phase.
    /// </summary>
    public PhaseCounterConfiguration Running { get; } = new();

    public static Action<ICounterProbe> DefaultAssertCounterData(PhaseCounterConfiguration configuration) =>
        probe =>
        {
            if (probe is NavigationProbe or CounterDataCollector)
            {
                var executeThreshold = probe is NavigationProbe
                    ? configuration.DbCommandExecutionRepetitionPerNavigationThreshold
                    : configuration.DbCommandExecutionRepetitionThreshold;
                var executeThresholdName = probe is NavigationProbe
                    ? nameof(configuration.DbCommandExecutionRepetitionPerNavigationThreshold)
                    : nameof(configuration.DbCommandExecutionRepetitionThreshold);
                var readThreshold = probe is NavigationProbe
                    ? configuration.DbReaderReadPerNavigationThreshold
                    : configuration.DbReaderReadThreshold;
                var readThresholdName = probe is NavigationProbe
                    ? nameof(configuration.DbReaderReadPerNavigationThreshold)
                    : nameof(configuration.DbReaderReadThreshold);

                AssertIntegerCounterValue<DbExecuteCounterKey>(probe, executeThresholdName, executeThreshold);
                AssertIntegerCounterValue<DbReadCounterKey>(probe, readThresholdName, readThreshold);
            }
        };

    public static void AssertIntegerCounterValue<TKey>(ICounterProbe probe, string thresholdName, int threshold)
        where TKey : ICounterKey =>
        probe.Counters.Keys
            .OfType<TKey>()
            .ForEach(key =>
            {
                if (probe.Counters[key] is IntegerCounterValue counterValue
                    && counterValue.Value > threshold)
                {
                    throw new CounterThresholdException(
                        probe,
                        key,
                        counterValue,
                        $"Counter value is greater then {thresholdName}, threshold: {threshold.ToTechnicalString()}");
                }
            });
}
