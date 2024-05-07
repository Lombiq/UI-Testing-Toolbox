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
    private readonly ConcurrentBag<ICounterProbe> _probes = [];
    private readonly ConcurrentBag<Exception> _postponedCounterExceptions = [];
    public override bool IsAttached => true;
    public Action<ICounterDataCollector, ICounterProbe> AssertCounterData { get; set; }
    public string Phase { get; set; }
    public bool IsEnabled { get; set; }

    public CounterDataCollector(ITestOutputHelper testOutputHelper) =>
        _testOutputHelper = testOutputHelper;

    public void AttachProbe(ICounterProbe probe)
    {
        if (!IsEnabled)
        {
            return;
        }

        probe.CaptureCompleted = ProbeCaptureCompleted;
        _probes.Add(probe);
    }

    public void Reset()
    {
        _probes.Clear();
        _postponedCounterExceptions.Clear();
        Clear();
    }

    public override void Increment(ICounterKey counter)
    {
        if (!IsEnabled)
        {
            return;
        }

        _probes.Where(probe => probe.IsAttached)
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

    public void AssertCounter(ICounterProbe probe) => AssertCounterData?.Invoke(this, probe);

    public void AssertCounter()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!_postponedCounterExceptions.IsEmpty)
        {
            throw new AggregateException(
                "There were exceptions out of the test execution context.",
                _postponedCounterExceptions);
        }

        AssertCounter(this);
    }

    public void PostponeCounterException(Exception exception) => _postponedCounterExceptions.Add(exception);

    private void ProbeCaptureCompleted(ICounterProbe probe) =>
        probe.Dump().ForEach(_testOutputHelper.WriteLine);
}
