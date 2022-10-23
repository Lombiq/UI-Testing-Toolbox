using System;

namespace Lombiq.Tests.UI.Services.Counters.Value;

public class IntegerCounterValue : CounterValue<int>
{
    public override string ToString() => Value.ToTechnicalString();
}
