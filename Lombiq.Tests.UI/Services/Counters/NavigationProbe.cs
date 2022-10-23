using System;

namespace Lombiq.Tests.UI.Services.Counters;

public class NavigationProbe : CounterProbe
{
    public Uri AbsoluteUri { get; init; }

    public NavigationProbe(ICounterDataCollector counterDataCollector, Uri absoluteUri)
        : base(counterDataCollector) =>
        AbsoluteUri = absoluteUri;

    public override string DumpHeadline() => $"{nameof(NavigationProbe)}, AbsoluteUri={AbsoluteUri}";
}
