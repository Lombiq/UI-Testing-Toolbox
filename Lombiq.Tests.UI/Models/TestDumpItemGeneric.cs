using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public class TestDumpItemGeneric<TContent> : ITestDumpItem
{
    private readonly TContent _content;
    private readonly Func<TContent, Task<Stream>> _getStream;
    private readonly Action<TContent> _dispose;
    private bool _disposed;

    public TestDumpItemGeneric(
        TContent content,
        Func<TContent, Task<Stream>> getStream = null,
        Action<TContent> dispose = null)
    {
        if (content is not Stream && getStream == null)
        {
            throw new ArgumentException(
                $"{nameof(content)} must be of type {nameof(Stream)} or {nameof(getStream)} must not be null.");
        }

        _content = content;
        _getStream = getStream;
        _dispose = dispose;
    }

    public Task<Stream> GetStreamAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_content is Stream stream && _getStream == null)
        {
            return Task.FromResult(stream);
        }

        return _getStream(_content);
    }

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
