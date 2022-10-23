using Lombiq.Tests.UI.Services.Counters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services;

public sealed class CounterDataCollector : CounterProbeBase, ICounterDataCollector
{
    private readonly ConcurrentBag<ICounterProbe> _probes = new();
    public override bool IsRunning => true;
    public Action<ICounterProbe> AssertCounterData { get; set; }

    public void AttachProbe(ICounterProbe probe) => _probes.Add(probe);

    public void Reset()
    {
        _probes.Clear();
        Clear();
    }

    public override void Increment(ICounterKey counter)
    {
        _probes.SelectWhere(probe => probe, probe => probe.IsRunning)
            .ForEach(probe => probe.Increment(counter));

        base.Increment(counter);
    }

    public override string DumpHeadline() => nameof(CounterDataCollector);
    public override string Dump() => DumpHeadline();
    public void AssertCounter(ICounterProbe probe) => AssertCounterData?.Invoke(probe);
    public void AssertCounter() => AssertCounter(this);
}
