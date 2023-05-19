using Lombiq.Tests.UI.Services.Counters.Extensions;
using System;

namespace Lombiq.Tests.UI.Services.Counters;

public sealed class PageLoadProbe : CounterProbe, IRelativeUrlConfigurationKey
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

    #region IRelativeUrlConfigurationKey implementation

    Uri IRelativeUrlConfigurationKey.Url => AbsoluteUri;
    bool IRelativeUrlConfigurationKey.ExactMatch => false;
    bool IEquatable<ICounterConfigurationKey>.Equals(ICounterConfigurationKey other) =>
        this.EqualsWith(other as IRelativeUrlConfigurationKey);

    #endregion
}
