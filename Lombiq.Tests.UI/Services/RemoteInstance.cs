using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public sealed class RemoteInstance : IWebApplicationInstance
{
    public bool SupportsServices => false;
    public bool SupportsPausing => false;
    public bool SupportsLogs => false;

    public OrchardCoreUITestExecutorConfiguration Configuration { get; }
    public IServiceProvider Services { get; }

    private readonly Uri _baseUri;

    public RemoteInstance(Uri baseUri, OrchardCoreUITestExecutorConfiguration configuration)
    {
        Configuration = configuration;
        Services = new ServiceCollection().BuildServiceProvider();
        _baseUri = baseUri;
    }

    public Task<Uri> StartUpAsync() => Task.FromResult(_baseUri);

    public Task<IEnumerable<IApplicationLog>> GetLogsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<IApplicationLog>());
    public Task ClearLogsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    [Obsolete($"Use {nameof(IWebApplicationInstance.Services)} instead.")]
    public TService GetRequiredService<TService>() => throw new NotSupportedException();
    public Task PauseAsync() => Task.CompletedTask;
    public Task ResumeAsync() => Task.CompletedTask;
    public Task TakeSnapshotAsync(string snapshotDirectoryPath) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
