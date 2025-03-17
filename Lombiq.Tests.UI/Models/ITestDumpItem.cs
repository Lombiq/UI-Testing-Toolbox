using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Represents an item, including the corresponding file, that's in the test's dump.
/// </summary>
public interface ITestDumpItem : IDisposable
{
    /// <summary>
    /// Gets the <see cref="Stream"/> that contains the content of the test dump item.
    /// </summary>
    /// <returns>The <see cref="Stream"/> that contains the content of the test dump item.</returns>
    Task<Stream> GetStreamAsync();
}
