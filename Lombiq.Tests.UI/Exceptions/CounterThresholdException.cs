using Lombiq.Tests.UI.Services.Counters;
using System;
using System.Text;

namespace Lombiq.Tests.UI.Exceptions;

public class CounterThresholdException : Exception
{
    public CounterThresholdException()
    {
    }

    public CounterThresholdException(string message)
        : this(probe: null, counter: null, value: null, message)
    {
    }

    public CounterThresholdException(string message, Exception innerException)
        : this(probe: null, counter: null, value: null, message, innerException)
    {
    }

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
        string message)
    {
        var builder = new StringBuilder();
        if (probe is not null) builder.AppendLine(probe.DumpHeadline());
        if (counter is not null) builder.AppendLine(counter.Dump());
        if (value is not null) builder.AppendLine(value.Dump());
        if (!string.IsNullOrEmpty(message)) builder.AppendLine(message);

        return builder.ToString();
    }
}
