using Lombiq.Tests.UI.Services.Counters.Extensions;
using System;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public sealed class RelativeUrlConfigurationKey : IRelativeUrlConfigurationKey
{
    public Uri Url { get; private init; }
    public bool ExactMatch { get; private init; }

    public RelativeUrlConfigurationKey(Uri url, bool exactMatch = true)
    {
        Url = url;
        ExactMatch = exactMatch;
    }

    public bool Equals(ICounterConfigurationKey other) =>
        this.EqualsWith(other as IRelativeUrlConfigurationKey);

    public override int GetHashCode() =>
        (Url, ExactMatch).GetHashCode();
}
