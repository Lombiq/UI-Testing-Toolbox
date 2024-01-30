using Lombiq.Tests.UI.Services.Counters.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterConfiguration
{
    private const string WorkflowTypeStartActivitiesQuery =
        "SELECT DISTINCT [Document].* FROM [Document] INNER JOIN [WorkflowTypeStartActivitiesIndex]"
        + " AS [WorkflowTypeStartActivitiesIndex_a1]"
        + " ON [WorkflowTypeStartActivitiesIndex_a1].[DocumentId] = [Document].[Id]"
        + " WHERE (([WorkflowTypeStartActivitiesIndex_a1].[StartActivityName] = @p0)"
        + " and ([WorkflowTypeStartActivitiesIndex_a1].[IsEnabled] = @p1))";

    /// <summary>
    /// Gets or sets the counter assertion method.
    /// </summary>
    public Action<ICounterDataCollector, ICounterProbe> AssertCounterData { get; set; }

    /// <summary>
    /// Gets or sets the exclude filter. Can be used to exclude counted values before assertion.
    /// </summary>
    public Func<ICounterKey, bool> ExcludeFilter { get; set; } = DefaultExcludeFilter;

    /// <summary>
    /// Gets or sets threshold configuration used under navigation requests. See:
    /// <see cref="UI.Extensions.NavigationUITestContextExtensions.GoToAbsoluteUrlAsync(UITestContext, Uri, bool)"/>.
    /// See: <see cref="NavigationProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration NavigationThreshold { get; set; } = new CounterThresholdConfiguration
    {
        DbCommandIncludingParametersExecutionCountThreshold = 11,
        DbCommandExcludingParametersExecutionThreshold = 22,
        DbReaderReadThreshold = 11,
    };

    /// <summary>
    /// Gets or sets threshold configuration used per <see cref="YesSql.ISession"/> lifetime. See:
    /// <see cref="SessionProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration SessionThreshold { get; set; } = new CounterThresholdConfiguration
    {
        DbCommandIncludingParametersExecutionCountThreshold = 22,
        DbCommandExcludingParametersExecutionThreshold = 44,
        DbReaderReadThreshold = 11,
    };

    /// <summary>
    /// Gets or sets threshold configuration used per page load. See: <see cref="PageLoadProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration PageLoadThreshold { get; set; } = new CounterThresholdConfiguration
    {
        DbCommandIncludingParametersExecutionCountThreshold = 22,
        DbCommandExcludingParametersExecutionThreshold = 44,
        DbReaderReadThreshold = 11,
    };

    public static IEnumerable<ICounterKey> DefaultExcludeList { get; } = new List<ICounterKey>
    {
        new DbCommandExecuteCounterKey(
            WorkflowTypeStartActivitiesQuery,
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentCreatedEvent"),
                new("p1", value: true),
            }),
        new DbCommandExecuteCounterKey(
            WorkflowTypeStartActivitiesQuery,
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentPublishedEvent"),
                new("p1", value: true),
            }),
        new DbCommandExecuteCounterKey(
            WorkflowTypeStartActivitiesQuery,
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentUpdatedEvent"),
                new("p1", value: true),
            }),
    };

    public static bool DefaultExcludeFilter(ICounterKey key) => DefaultExcludeList.Contains(key);
}
