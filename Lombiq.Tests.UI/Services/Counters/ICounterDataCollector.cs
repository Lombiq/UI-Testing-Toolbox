using System;

namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a data collector which collects and asserts data from probes attached to it.
/// </summary>
public interface ICounterDataCollector : ICounterProbe
{
    /// <summary>
    /// Attaches a <see cref="ICounterProbe"/> instance given by <paramref name="probe"/> to the current collector instance.
    /// </summary>
    /// <param name="probe">The <see cref="ICounterProbe"/> instance to attach.</param>
    void AttachProbe(ICounterProbe probe);

    /// <summary>
    /// Resets the collected counters and probes.
    /// </summary>
    void Reset();

    /// <summary>
    /// Asserts the data collected by <paramref name="probe"/>.
    /// </summary>
    /// <param name="probe">The <see cref="ICounterProbe"/> instance to assert.</param>
    void AssertCounter(ICounterProbe probe);

    /// <summary>
    /// Asserts the data collected by the instance.
    /// </summary>
    void AssertCounter();

    /// <summary>
    /// Postpones exception thrown by a counter when the exception was thrown from the test context.
    /// </summary>
    void PostponeCounterException(Exception exception);
}
