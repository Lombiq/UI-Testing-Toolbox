using Lombiq.Tests.UI.Services.Counters.Value;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters;

public abstract class CounterProbeBase : ICounterProbe
{
    private readonly ConcurrentDictionary<ICounterKey, ICounterValue> _counters = new();

    public abstract bool IsAttached { get; }
    public Action<ICounterProbe> CaptureCompleted { get; set; }
    public IDictionary<ICounterKey, ICounterValue> Counters => _counters;

    protected void Clear() => _counters.Clear();

    public virtual void Increment(ICounterKey counter) =>
        _counters.AddOrUpdate(
            counter,
            new IntegerCounterValue { Value = 1 },
            (_, current) => TryConvertAndUpdate<IntegerCounterValue>(current, current => current.Value++));

    public abstract string DumpHeadline();

    public abstract IEnumerable<string> Dump();

    public virtual IEnumerable<string> DumpSummary()
    {
        if (!Counters.Any())
        {
            return Enumerable.Empty<string>();
        }

        var lines = new List<string>
        {
            "Summary:",
        };

        lines.AddRange(
            Counters.GroupBy(entry => entry.Key.GetType())
                .SelectMany(keyGroup =>
                {
                    var keyGroupLines = new List<string>
                    {
                        keyGroup.First().Key.DisplayName,
                    };

                    var integerCounter = keyGroup.Select(entry => entry.Value)
                        .OfType<IntegerCounterValue>()
                        .Select(value => value.Value)
                        .Max();
                    keyGroupLines.Add($"\tmaximum: {integerCounter.ToTechnicalString()}"); // #spell-check-ignore-line

                    return keyGroupLines.Select(line => $"\t{line}");
                }));

        return lines;
    }

    protected virtual void OnCaptureCompleted() =>
        CaptureCompleted?.Invoke(this);

    private static TCounter TryConvertAndUpdate<TCounter>(ICounterValue counter, Action<TCounter> update)
    {
        if (counter is not TCounter value)
        {
            throw new ArgumentException(
                $"The type of ${nameof(counter)} is not compatible with ${typeof(TCounter).Name}.");
        }

        update.Invoke(value);

        return value;
    }
}
