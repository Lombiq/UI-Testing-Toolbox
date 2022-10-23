using System;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a key in <see cref="ICounterProbe.Counters"/>.
/// </summary>
public interface ICounterKey : IEquatable<ICounterKey>
{
    /// <summary>
    /// Dumps the key content to a human readable format.
    /// </summary>
    /// <returns>A human readable string representation of instance.</returns>
    string Dump();
}
