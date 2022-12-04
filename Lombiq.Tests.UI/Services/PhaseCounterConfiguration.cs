using Lombiq.Tests.UI.Services.Counters;
using Lombiq.Tests.UI.Services.Counters.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services;

public class PhaseCounterConfiguration
{
    public Action<ICounterProbe> AssertCounterData { get; set; }
    public Func<ICounterKey, bool> ExcludeFilter { get; set; } = DefaultExcludeFilter;
    public int DbCommandExecutionRepetitionPerNavigationThreshold { get; set; } = 11;
    public int DbCommandExecutionRepetitionThreshold { get; set; } = 22;
    public int DbReaderReadPerNavigationThreshold { get; set; } = 11;
    public int DbReaderReadThreshold { get; set; } = 11;

    public static IEnumerable<ICounterKey> DefaultExcludeList { get; } = new List<ICounterKey>
    {
        new DbExecuteCounterKey(
            "SELECT DISTINCT [Document].* FROM [Document] INNER JOIN [WorkflowTypeStartActivitiesIndex]"
            + " AS [WorkflowTypeStartActivitiesIndex_a1]"
            + " ON [WorkflowTypeStartActivitiesIndex_a1].[DocumentId] = [Document].[Id]"
            + " WHERE (([WorkflowTypeStartActivitiesIndex_a1].[StartActivityName] = @p0)"
            + " and ([WorkflowTypeStartActivitiesIndex_a1].[IsEnabled] = @p1))",
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentCreatedEvent"),
                new("p1", value: true),
            }),
        new DbExecuteCounterKey(
            "SELECT DISTINCT [Document].* FROM [Document] INNER JOIN [WorkflowTypeStartActivitiesIndex]"
            + " AS [WorkflowTypeStartActivitiesIndex_a1]"
            + " ON [WorkflowTypeStartActivitiesIndex_a1].[DocumentId] = [Document].[Id]"
            + " WHERE (([WorkflowTypeStartActivitiesIndex_a1].[StartActivityName] = @p0)"
            + " and ([WorkflowTypeStartActivitiesIndex_a1].[IsEnabled] = @p1))",
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentPublishedEvent"),
                new("p1", value: true),
            }),
        new DbExecuteCounterKey(
            "SELECT DISTINCT [Document].* FROM [Document] INNER JOIN [WorkflowTypeStartActivitiesIndex]"
            + " AS [WorkflowTypeStartActivitiesIndex_a1]"
            + " ON [WorkflowTypeStartActivitiesIndex_a1].[DocumentId] = [Document].[Id]"
            + " WHERE (([WorkflowTypeStartActivitiesIndex_a1].[StartActivityName] = @p0)"
            + " and ([WorkflowTypeStartActivitiesIndex_a1].[IsEnabled] = @p1))",
            new List<KeyValuePair<string, object>>
            {
                new("p0", "ContentUpdatedEvent"),
                new("p1", value: true),
            }),
    };

    public static bool DefaultExcludeFilter(ICounterKey key) => DefaultExcludeList.Contains(key);
}
