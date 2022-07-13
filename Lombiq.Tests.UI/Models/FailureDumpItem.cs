using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public class FailureDumpItem : IFailureDumpItem
{
    private readonly Func<Task<Stream>> _getStream;
    private readonly Action _dispose;
    private bool _disposed;

    public FailureDumpItem(
        Func<Task<Stream>> getStream,
        Action dispose = null)
    {
        _getStream = getStream ?? throw new ArgumentNullException(nameof(getStream));
        _dispose = dispose;
    }

    public Task<Stream> GetStreamAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FailureDumpItem));
        }

        return _getStream();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dispose?.Invoke();
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
