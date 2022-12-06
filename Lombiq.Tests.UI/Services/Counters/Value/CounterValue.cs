using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters.Value;

public abstract class CounterValue<TValue> : ICounterValue
    where TValue : struct
{
    public TValue Value { get; set; }

    public virtual IEnumerable<string> Dump() =>
        new[] { FormattableString.Invariant($"{GetType().Name} value: {Value}") };
}
