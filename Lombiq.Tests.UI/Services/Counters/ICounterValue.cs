using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a value in <see cref="ICounterProbe.Counters"/>.
/// </summary>
public interface ICounterValue
{
    /// <summary>
    /// Gets the display name of the value.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Dumps the value content to a human-readable format.
    /// </summary>
    /// <returns>A human-readable representation of the instance.</returns>
    IEnumerable<string> Dump();
}
