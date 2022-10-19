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
    IEnumerable<IApplicationLog> GetLogs(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get service of type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// There is no service of type <typeparamref name="TService"/>.
    /// </exception>
    TService GetRequiredService<TService>();
}

/// <summary>
/// An abstraction over a log, be it in the form of a file or something else.
/// </summary>
public interface IApplicationLog
{
    /// <summary>
    /// Gets the name of the log, such as the file name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns the content of the log, in case of log files reads the file contents.
    /// </summary>
    /// <returns>The contents.</returns>
    Task<string> GetContentAsync();

    /// <summary>
    /// Removes the log if possible.
    /// </summary>
    void Remove();
}
