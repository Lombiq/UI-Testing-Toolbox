using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters;

public abstract class CounterProbe : CounterProbeBase, IDisposable
{
    private bool _disposed;

    public override bool IsAttached => !_disposed;
    public ICounterDataCollector CounterDataCollector { get; init; }

    public override IEnumerable<string> Dump()
    {
        var lines = new List<string>
        {
            DumpHeadline(),
        };

        lines.AddRange(
            Counters.SelectMany(entry =>
                entry.Key.Dump()
                    .Concat(entry.Value.Dump().Select(line => $"\t{line}")))
            .Concat(DumpSummary())
            .Select(line => $"\t{line}"));

        return lines;
    }

    protected CounterProbe(ICounterDataCollector counterDataCollector)
    {
        CounterDataCollector = counterDataCollector;
        CounterDataCollector.AttachProbe(this);
    }

    protected virtual void OnAssertData() =>
        CounterDataCollector.AssertCounter(this);

    protected virtual void OnDisposing()
    {
    }

    protected virtual void OnDisposed()
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            OnDisposing();
            try { OnAssertData(); }
            finally
            {
                if (disposing)
                {
                    OnDisposed();
                }

                _disposed = true;
                OnCaptureCompleted();
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
