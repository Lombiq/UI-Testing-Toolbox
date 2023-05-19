using Lombiq.Tests.UI.Services.Counters.Extensions;
using System;

namespace Lombiq.Tests.UI.Services.Counters;

public sealed class NavigationProbe : CounterProbe, IRelativeUrlConfigurationKey
{
    public Uri AbsoluteUri { get; init; }

    public NavigationProbe(ICounterDataCollector counterDataCollector, Uri absoluteUri)
        : base(counterDataCollector) =>
        AbsoluteUri = absoluteUri;

    public override string DumpHeadline() => $"{nameof(NavigationProbe)}, AbsoluteUri = {AbsoluteUri}";

    #region IRelativeUrlConfigurationKey implementation

    Uri IRelativeUrlConfigurationKey.Url => AbsoluteUri;
    bool IRelativeUrlConfigurationKey.ExactMatch => false;
    bool IEquatable<ICounterConfigurationKey>.Equals(ICounterConfigurationKey other) =>
        this.EqualsWith(other as IRelativeUrlConfigurationKey);

    #endregion
}
