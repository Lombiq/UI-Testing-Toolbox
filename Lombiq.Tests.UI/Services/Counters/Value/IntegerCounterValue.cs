using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters.Value;

public class IntegerCounterValue : CounterValue<int>
{
    public override IEnumerable<string> Dump() => new[]
    {
        $"{DisplayName}: {this}",
    };

    public override string ToString() => Value.ToTechnicalString();
}
