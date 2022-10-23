using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a probe for the counter infrastructure.
/// </summary>
public interface ICounterProbe
{
    /// <summary>
    /// Gets a value indicating whether the instance is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the collected values.
    /// </summary>
    IDictionary<ICounterKey, ICounterValue> Counters { get; }

    /// <summary>
    /// Increments the <see cref="Value.IntegerCounterValue"/> selected by <paramref name="counter"/>. If the
    /// <see cref="Value.IntegerCounterValue"/> does not exists, creates a new instance.
    /// </summary>
    /// <param name="counter">The counter key.</param>
    void Increment(ICounterKey counter);

    /// <summary>
    /// Dumps the probe headline to a human readable format.
    /// </summary>
    /// <returns>A human readable string representation of instance in one line.</returns>
    public string DumpHeadline();

    /// <summary>
    /// Dumps the probe content to a human readable format.
    /// </summary>
    /// <returns>A human readable string representation of instance.</returns>
    string Dump();
}
