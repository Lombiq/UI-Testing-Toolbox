using System;

namespace Lombiq.Tests.UI.Services.Counters.Value;

public abstract class CounterValue<TValue> : ICounterValue
    where TValue : struct
{
    public TValue Value { get; set; }

    public virtual string Dump() =>
        FormattableString.Invariant($"{GetType().Name} value: {Value}");
}
