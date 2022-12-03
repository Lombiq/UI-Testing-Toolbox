using Lombiq.Tests.UI.Services.Counters;
using System;

namespace Lombiq.Tests.UI.Services;

public class PhaseCounterConfiguration
{
    public Action<ICounterProbe> AssertCounterData { get; set; }
    public Func<ICounterKey, bool> ExcludeFilter { get; set; } = CounterConfiguration.DefaultExcludeFilter;
    public int DbCommandExecutionRepetitionPerNavigationThreshold { get; set; } = 11;
    public int DbCommandExecutionRepetitionThreshold { get; set; } = 22;
    public int DbReaderReadPerNavigationThreshold { get; set; } = 11;
    public int DbReaderReadThreshold { get; set; } = 11;
}
