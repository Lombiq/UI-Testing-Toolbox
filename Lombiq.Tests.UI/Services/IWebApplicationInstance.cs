using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// A web application instance, like an Orchard Core app executing via dotnet.exe.
    /// </summary>
    public interface IWebApplicationInstance : IAsyncDisposable
    {
        /// <summary>
        /// Launches the web application.
        /// </summary>
        /// <returns>The starting URL of the webapp, such as the home page.</returns>
        Task<Uri> StartUpAsync();

        /// <summary>
        /// Stops running the application without disposing it.
        /// </summary>
        Task PauseAsync();

        /// <summary>
        /// Starts the application back up again.
        /// </summary>
        Task ResumeAsync();

        /// <summary>
        /// Pauses and saves the state of the application.
        /// </summary>
        /// <param name="snapshotDirectoryPath">The save location.</param>
        Task TakeSnapshotAsync(string snapshotDirectoryPath);

        /// <summary>
        /// Reads all the application logs.
        /// </summary>
        /// <returns>The collection of log names and their contents.</returns>
        IEnumerable<IApplicationLog> GetLogs();
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
    }
}
