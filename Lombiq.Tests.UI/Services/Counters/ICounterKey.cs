using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a key in <see cref="ICounterProbe.Counters"/>.
/// </summary>
public interface ICounterKey : IEquatable<ICounterKey>
{
    /// <summary>
    /// Dumps the key content to a human-readable format.
    /// </summary>
    /// <returns>A human-readable representation of instance.</returns>
    IEnumerable<string> Dump();
}
