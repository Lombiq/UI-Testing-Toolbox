namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a value in <see cref="ICounterProbe.Counters"/>.
/// </summary>
public interface ICounterValue
{
    /// <summary>
    /// Dumps the value content to a human readable format.
    /// </summary>
    /// <returns>A human readable string representation of instance.</returns>
    string Dump();
}
