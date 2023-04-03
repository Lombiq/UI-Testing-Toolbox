using Lombiq.Tests.UI.Services.Counters.Extensions;
using System;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public sealed class RelativeUrlConfigurationKey : IRelativeUrlConfigurationKey
{
    public Uri Url { get; private init; }

    public RelativeUrlConfigurationKey(Uri url) =>
        Url = url;

    public bool Equals(ICounterConfigurationKey other) =>
        this.EqualsWith(other as IRelativeUrlConfigurationKey);
}
