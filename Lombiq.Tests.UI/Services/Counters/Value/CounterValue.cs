using System.Collections.Generic;
using System.Globalization;

namespace Lombiq.Tests.UI.Services.Counters.Value;

public abstract class CounterValue<TValue> : ICounterValue
    where TValue : struct
{
    public TValue Value { get; set; }

    public virtual IEnumerable<string> Dump() =>
        new[] { string.Create(CultureInfo.InvariantCulture, $"{GetType().Name} value: {Value}") };
}
