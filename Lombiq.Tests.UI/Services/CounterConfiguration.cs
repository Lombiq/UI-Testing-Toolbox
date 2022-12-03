using Atata;
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

                AssertIntegerCounterValue<DbExecuteCounterKey>(
                    probe,
                    configuration.ExcludeFilter ?? (key => false),
                    executeThresholdName,
                    executeThreshold);
                AssertIntegerCounterValue<DbReadCounterKey>(
                    probe,
                    configuration.ExcludeFilter ?? (key => false),
                    readThresholdName,
                    readThreshold);
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

    public static bool DefaultExcludeFilter(ICounterKey key)
    {
        if (key is DbExecuteCounterKey dbExecuteCounter)
        {
            if (dbExecuteCounter.CommandText == @"SELECT DISTINCT [Document].* FROM [Document] INNER JOIN [WorkflowTypeStartActivitiesIndex] AS [WorkflowTypeStartActivitiesIndex_a1] ON [WorkflowTypeStartActivitiesIndex_a1].[DocumentId] = [Document].[Id] WHERE (([WorkflowTypeStartActivitiesIndex_a1].[StartActivityName] = @p0) and ([WorkflowTypeStartActivitiesIndex_a1].[IsEnabled] = @p1))")
            {
                return true;
            }
        }

        return false;
    }
}
