using Lombiq.Tests.UI.Services.Counters;
using System;
using System.Text;

namespace Lombiq.Tests.UI.Exceptions;

// We need constructors with required informations.
#pragma warning disable CA1032 // Implement standard exception constructors
public class CounterThresholdException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public CounterThresholdException(
        ICounterProbe probe,
        ICounterKey counter,
        ICounterValue value)
        : this(probe, counter, value, message: null, innerException: null)
    {
    }

    public CounterThresholdException(
        ICounterProbe probe,
        ICounterKey counter,
        ICounterValue value,
        string message)
        : this(probe, counter, value, message, innerException: null)
    {
    }

    public CounterThresholdException(
        ICounterProbe probe,
        ICounterKey counter,
        ICounterValue value,
        string message,
        Exception innerException)
        : base(FormatMessage(probe, counter, value, message), innerException)
    {
    }

    private static string FormatMessage(
        ICounterProbe probe,
        ICounterKey counter,
        ICounterValue value,
        string message) =>
        new StringBuilder()
            .AppendLine(probe.DumpHeadline())
            .AppendLine(counter.Dump())
            .AppendLine(value.Dump())
            .AppendLine(message ?? string.Empty)
            .ToString();
}
