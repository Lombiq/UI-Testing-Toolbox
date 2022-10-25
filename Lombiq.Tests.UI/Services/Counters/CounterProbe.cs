using System;
using System.Text;

namespace Lombiq.Tests.UI.Services.Counters;

public abstract class CounterProbe : CounterProbeBase, IDisposable
{
    private bool _disposed;

    public override bool IsRunning => !_disposed;
    public ICounterDataCollector CounterDataCollector { get; init; }

    public override string Dump()
    {
        var builder = new StringBuilder();

        builder.AppendLine(DumpHeadline());

        foreach (var entry in Counters)
        {
            builder.AppendLine(entry.Key.Dump())
                .AppendLine(entry.Value.Dump());
        }

        return builder.ToString();
    }

    protected CounterProbe(ICounterDataCollector counterDataCollector)
    {
        CounterDataCollector = counterDataCollector;
        CounterDataCollector.AttachProbe(this);
    }

    protected virtual void OnAssertData() =>
        CounterDataCollector.AssertCounter(this);

    protected virtual void OnDispose()
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            try { OnAssertData(); }
            finally
            {
                if (disposing)
                {
                    OnDispose();
                }

                _disposed = true;
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
