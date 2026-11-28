using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// A web application instance, like an Orchard Core app executing via <c>dotnet</c>.
/// </summary>
public interface IWebApplicationInstance : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the application instance supports access to the service provider. If <see
    /// langword="true"/>, <see cref="Services"/> can be used.
    /// </summary>
    bool SupportsServices { get; }

    /// <summary>
    /// Gets a value indicating whether the application instance can be paused and resumed. If <see langword="true"/>,
    /// <see cref="PauseAsync"/>, <see cref="ResumeAsync"/> and <see cref="TakeSnapshotAsync"/> can be used.
    /// </summary>
    bool SupportsPausing { get; }

    /// <summary>
    /// Gets a value indicating whether the application instance supports access to the logs. If <see langword="true"/>,
    /// <see cref="GetLogsAsync"/> can be used (otherwise it should always return an empty collection).
    /// </summary>
    bool SupportsLogs { get; }

    /// <summary>
    /// Gets the test configuration used to initialize the instance.
    /// </summary>
    OrchardCoreUITestExecutorConfiguration Configuration { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> created by the server associated with this
    /// <see cref="IWebApplicationInstance"/>.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Launches the web application.
    /// </summary>
    /// <returns>The starting URL of the web app, such as the home page.</returns>
    Task<Uri> StartUpAsync();

    /// <summary>
    /// Stops running the application without disposing it. It can be restarted with <see cref="ResumeAsync()"/>.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Starts the application back up again after it was stopped with <see cref="PauseAsync()"/>.
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    /// Pauses (see <see cref="PauseAsync"/>) and saves the state of the application. It can be restarted with <see
    /// cref="ResumeAsync()"/>.
    /// </summary>
    /// <param name="snapshotDirectoryPath">The save location.</param>
    Task TakeSnapshotAsync(string snapshotDirectoryPath);

    /// <summary>
    /// Reads all the application logs.
    /// </summary>
    /// <returns>The collection of log names and their contents.</returns>
    Task<IEnumerable<IApplicationLog>> GetLogsAsync(CancellationToken cancellationToken = default);
}
