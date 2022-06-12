using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Represents an item, including the corresponding file, that's in the test's failure dump.
/// </summary>
public interface IFailureDumpItem : IDisposable
{
    /// <summary>
    /// Gets the <see cref="Stream"/> associated to failure dump item.
    /// </summary>
    /// <returns>The <see cref="Stream"/> associated to failure dump item.</returns>
    Task<Stream> GetStreamAsync();
}
