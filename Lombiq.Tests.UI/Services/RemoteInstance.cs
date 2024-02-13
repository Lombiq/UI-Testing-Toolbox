using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public sealed class RemoteInstance : IWebApplicationInstance
{
    public IServiceProvider Services => throw new NotSupportedException();

    private readonly Uri _baseUri;

    public RemoteInstance(Uri baseUri) => _baseUri = baseUri;

    public Task<Uri> StartUpAsync() => Task.FromResult(_baseUri);

    public IEnumerable<IApplicationLog> GetLogs(CancellationToken cancellationToken = default) => Enumerable.Empty<IApplicationLog>();
    public TService GetRequiredService<TService>() => throw new NotSupportedException();
    public Task PauseAsync() => throw new NotSupportedException();
    public Task ResumeAsync() => throw new NotSupportedException();
    public Task TakeSnapshotAsync(string snapshotDirectoryPath) => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
