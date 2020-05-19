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
        Task<Uri> StartUp();
        Task Pause();
        Task Resume();
        Task TakeSnapshot(string snapshotDirectoryPath);
        IEnumerable<IApplicationLog> GetLogs();
    }


    /// <summary>
    /// An abstraction over a log, be it in the form of a file or something else.
    /// </summary>
    public interface IApplicationLog
    {
        string Name { get; }
        Task<string> GetContent();
    }
}
