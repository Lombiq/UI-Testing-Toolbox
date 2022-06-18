using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public class FailureDumpItemGeneric<TContent> : IFailureDumpItem
{
    private readonly TContent _content;
    private readonly Func<TContent, Task<Stream>> _getStream;
    private readonly Action<TContent> _dispose;
    private readonly IEnumerable<Type> _inCaseOf;
    private bool _disposed;

    public FailureDumpItemGeneric(
        TContent content,
        Func<TContent, Task<Stream>> getStream = null,
        Action<TContent> dispose = null,
        IEnumerable<Type> inCaseOf = null)
    {
        if (content is not Stream && getStream == null)
        {
            throw new ArgumentException($"{nameof(content)} is not a Stream, {nameof(getStream)} can't be null");
        }

        _content = content;
        _getStream = getStream;
        _dispose = dispose;
        _inCaseOf = inCaseOf;
    }

    public Task<Stream> GetStreamAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FailureDumpItemGeneric<TContent>));
        }

        if (_content is Stream stream && _getStream == null)
        {
            return Task.FromResult(stream);
        }

        return _getStream(_content);
    }

    public bool EnsureCanDump(Exception exceptionThrown) =>
        _inCaseOf is null || _inCaseOf.Any(type => type == exceptionThrown.GetType());

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_content is IDisposable disposable && _dispose == null)
                {
                    disposable.Dispose();
                }
                else
                {
                    _dispose?.Invoke(_content);
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
