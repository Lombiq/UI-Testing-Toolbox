using System;

namespace Lombiq.Tests.UI.Services.Counters;

public class PageLoadProbe : CounterProbe
{
    public string RequestMethod { get; init; }
    public Uri AbsoluteUri { get; init; }

    public PageLoadProbe(ICounterDataCollector counterDataCollector, string requestMethod, Uri absoluteUri)
        : base(counterDataCollector)
    {
        RequestMethod = requestMethod;
        AbsoluteUri = absoluteUri;
    }

    public override string DumpHeadline() => $"{nameof(PageLoadProbe)}, [{RequestMethod}]{AbsoluteUri}";
}
