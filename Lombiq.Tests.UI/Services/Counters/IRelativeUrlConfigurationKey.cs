using System;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a relative URL configuration key in <see cref="Configuration.RunningPhaseCounterConfiguration"/>.
/// </summary>
public interface IRelativeUrlConfigurationKey : ICounterConfigurationKey
{
    /// <summary>
    /// Gets the URL.
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Gets a value indicating whether the URL should be matched exactly or substring match is enough.
    /// </summary>
    bool ExactMatch { get; }
}
