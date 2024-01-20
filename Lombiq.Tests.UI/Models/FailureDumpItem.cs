using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public class FailureDumpItem(
    Func<Task<Stream>> getStream,
    Action dispose = null) : IFailureDumpItem
{
    private readonly Func<Task<Stream>> _getStream = getStream ?? throw new ArgumentNullException(nameof(getStream));
    private bool _disposed;

    public Task<Stream> GetStreamAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _getStream();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                dispose?.Invoke();
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
