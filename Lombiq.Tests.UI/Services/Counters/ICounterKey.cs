using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a key in <see cref="ICounterProbe.Counters"/>.
/// </summary>
public interface ICounterKey : IEquatable<ICounterKey>
{
    /// <summary>
    /// Gets the display name of the key.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Dumps the key content to a human-readable format.
    /// </summary>
    /// <returns>A human-readable representation of the instance.</returns>
    IEnumerable<string> Dump();
}
