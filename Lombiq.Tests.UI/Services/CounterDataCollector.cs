using Lombiq.Tests.UI.Services.Counters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public sealed class CounterDataCollector : CounterProbeBase, ICounterDataCollector
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ConcurrentBag<ICounterProbe> _probes = new();
    public override bool IsRunning => true;
    public Action<ICounterProbe> AssertCounterData { get; set; }
    public string Phase { get; set; }

    public CounterDataCollector(ITestOutputHelper testOutputHelper) =>
        _testOutputHelper = testOutputHelper;

    public void AttachProbe(ICounterProbe probe)
    {
        probe.CaptureCompleted = ProbeCaptureCompleted;
        _probes.Add(probe);
    }

    public void Reset()
    {
        _probes.Clear();
        Clear();
    }

    public override void Increment(ICounterKey counter)
    {
        _probes.Where(probe => probe.IsRunning)
            .ForEach(probe => probe.Increment(counter));
        base.Increment(counter);
    }

    public override string DumpHeadline() => $"{nameof(CounterDataCollector)}, Phase = {Phase}";
    public override IEnumerable<string> Dump()
    {
        var lines = new List<string>
        {
            DumpHeadline(),
        };

        lines.AddRange(DumpSummary().Select(line => $"\t{line}"));

        return lines;
    }

    public void AssertCounter(ICounterProbe probe) => AssertCounterData?.Invoke(probe);
    public void AssertCounter() => AssertCounter(this);

    private void ProbeCaptureCompleted(ICounterProbe probe) =>
        probe.Dump().ForEach(line => _testOutputHelper.WriteLine(line));
}
