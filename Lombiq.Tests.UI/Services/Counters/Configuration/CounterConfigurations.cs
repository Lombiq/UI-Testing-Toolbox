using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services.Counters.Data;
using Lombiq.Tests.UI.Services.Counters.Extensions;
using Lombiq.Tests.UI.Services.Counters.Value;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterConfigurations
{
    /// <summary>
    /// Gets the counter configuration used in the setup phase of the web application.
    /// </summary>
    public PhaseCounterConfiguration Setup { get; } = new();

    /// <summary>
    /// Gets the counter configuration used in the running phase of the web application.
    /// </summary>
    public RunningPhaseCounterConfiguration Running { get; } = new();

    public static Action<ICounterDataCollector, ICounterProbe> DefaultAssertCounterData(
        PhaseCounterConfiguration configuration) =>
        (collector, probe) =>
        {
            var counterConfiguration = configuration as CounterConfiguration;
            if (counterConfiguration is RunningPhaseCounterConfiguration runningPhaseCounterConfiguration
                && probe is ICounterConfigurationKey counterConfigurationKey)
            {
                counterConfiguration = runningPhaseCounterConfiguration.GetMaybeByKey(counterConfigurationKey)
                    ?? configuration;
            }

            (CounterThresholdConfiguration Settings, string Name)? threshold = probe switch
            {
                NavigationProbe =>
                    (Settings: counterConfiguration.NavigationThreshold, Name: nameof(counterConfiguration.NavigationThreshold)),
                PageLoadProbe =>
                    (Settings: counterConfiguration.PageLoadThreshold, Name: nameof(counterConfiguration.PageLoadThreshold)),
                SessionProbe =>
                    (Settings: counterConfiguration.SessionThreshold, Name: nameof(counterConfiguration.SessionThreshold)),
                CounterDataCollector when counterConfiguration is PhaseCounterConfiguration phaseCounterConfiguration =>
                    (Settings: phaseCounterConfiguration.PhaseThreshold, Name: nameof(phaseCounterConfiguration.PhaseThreshold)),
                _ => null,
            };

            if (threshold is { } settings && !settings.Settings.Disable)
            {
                try
                {
                    AssertIntegerCounterValue<DbCommandExecuteCounterKey>(
                        probe,
                        counterConfiguration.ExcludeFilter ?? (key => false),
                        $"{settings.Name}.{nameof(settings.Settings.DbCommandExecutionThreshold)}",
                        settings.Settings.DbCommandExecutionThreshold);
                    AssertIntegerCounterValue<DbCommandTextExecuteCounterKey>(
                        probe,
                        counterConfiguration.ExcludeFilter ?? (key => false),
                        $"{settings.Name}.{nameof(settings.Settings.DbCommandTextExecutionThreshold)}",
                        settings.Settings.DbCommandTextExecutionThreshold);
                    AssertIntegerCounterValue<DbReaderReadCounterKey>(
                        probe,
                        counterConfiguration.ExcludeFilter ?? (key => false),
                        $"{settings.Name}.{nameof(settings.Settings.DbReaderReadThreshold)}",
                        settings.Settings.DbReaderReadThreshold);
                }
                catch (CounterThresholdException exception) when (probe is IOutOfTestContextCounterProbe)
                {
                    collector.PostponeCounterException(exception);
                }
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
