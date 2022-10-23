using Lombiq.Tests.UI.Services.Counters.Value;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

public abstract class CounterProbeBase : ICounterProbe
{
    private readonly ConcurrentDictionary<ICounterKey, ICounterValue> _counters = new();

    public abstract bool IsRunning { get; }
    public IDictionary<ICounterKey, ICounterValue> Counters => _counters;

    protected void Clear() => _counters.Clear();

    public virtual void Increment(ICounterKey counter) =>
        _counters.AddOrUpdate(
            counter,
            new IntegerCounterValue { Value = 1 },
            (_, current) => TryConvertAndUpdate<IntegerCounterValue>(current, current => current.Value++));

    public abstract string DumpHeadline();

    public abstract string Dump();

    private static TCounter TryConvertAndUpdate<TCounter>(ICounterValue counter, Action<TCounter> update)
    {
        if (counter is not TCounter value)
        {
            throw new ArgumentException(
                $"The type of ${nameof(counter)} is not compatible with ${typeof(TCounter).Name}");
        }

        update.Invoke(value);

        return value;
    }
}
