using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services.Counters.Data;
using Lombiq.Tests.UI.Services.Counters.Value;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterConfiguration
{
    /// <summary>
    /// Gets the counter configuration used in the setup phase of the web application.
    /// </summary>
    public PhaseCounterConfiguration Setup { get; } = new();

    /// <summary>
    /// Gets the counter configuration used in the running phase of the web application.
    /// </summary>
    public RunningPhaseCounterConfiguration Running { get; } = new();

    public static Action<ICounterProbe> DefaultAssertCounterData(PhaseCounterConfiguration configuration) =>
        probe =>
        {
            var phaseConfiguration = configuration;
            if (phaseConfiguration is RunningPhaseCounterConfiguration runningPhaseCounterConfiguration
                && probe is ICounterConfigurationKey counterConfigurationKey)
            {
                phaseConfiguration = runningPhaseCounterConfiguration.GetMaybe(counterConfigurationKey) ?? configuration;
            }

            (CounterThresholdConfiguration Settings, string Name)? threshold = probe switch
            {
                NavigationProbe =>
                    (Settings: phaseConfiguration.NavigationThreshold, Name: nameof(phaseConfiguration.NavigationThreshold)),
                PageLoadProbe =>
                    (Settings: phaseConfiguration.PageLoadThreshold, Name: nameof(phaseConfiguration.NavigationThreshold)),
                SessionProbe =>
                    (Settings: phaseConfiguration.SessionThreshold, Name: nameof(phaseConfiguration.NavigationThreshold)),
                CounterDataCollector =>
                    (Settings: phaseConfiguration.PhaseThreshold, Name: nameof(phaseConfiguration.NavigationThreshold)),
                _ => null,
            };

            if (threshold is { } settings && settings.Settings.Disable is not true)
            {
                AssertIntegerCounterValue<DbCommandExecuteCounterKey>(
                    probe,
                    phaseConfiguration.ExcludeFilter ?? (key => false),
                    $"{settings.Name}.{nameof(settings.Settings.DbCommandExecutionThreshold)}",
                    settings.Settings.DbCommandExecutionThreshold);
                AssertIntegerCounterValue<DbCommandTextExecuteCounterKey>(
                    probe,
                    phaseConfiguration.ExcludeFilter ?? (key => false),
                    $"{settings.Name}.{nameof(settings.Settings.DbCommandTextExecutionThreshold)}",
                    settings.Settings.DbCommandTextExecutionThreshold);
                AssertIntegerCounterValue<DbReaderReadCounterKey>(
                    probe,
                    phaseConfiguration.ExcludeFilter ?? (key => false),
                    $"{settings.Name}.{nameof(settings.Settings.DbReaderReadThreshold)}",
                    settings.Settings.DbReaderReadThreshold);
            }
        };

    public static void AssertIntegerCounterValue<TKey>(
        ICounterProbe probe,
        Func<ICounterKey, bool> excludeFilter,
        string thresholdName,
        int threshold)
        where TKey : ICounterKey =>
        probe.Counters.Keys
            .OfType<TKey>()
            .Where(key => !excludeFilter(key))
            .ForEach(key =>
            {
                if (probe.Counters[key] is IntegerCounterValue counterValue
                    && counterValue.Value > threshold)
                {
                    throw new CounterThresholdException(
                        probe,
                        key,
                        counterValue,
                        $"Counter value is greater then {thresholdName}, threshold: {threshold.ToTechnicalString()}.");
                }
            });
}
