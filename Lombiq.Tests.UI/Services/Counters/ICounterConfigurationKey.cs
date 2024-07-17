using System;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a key in <see cref="Configuration.RunningPhaseCounterConfiguration"/>.
/// </summary>
public interface ICounterConfigurationKey : IEquatable<ICounterConfigurationKey>
{
}
