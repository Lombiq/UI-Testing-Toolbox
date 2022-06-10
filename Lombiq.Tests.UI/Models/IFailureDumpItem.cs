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
    /// Return a <see cref="Stream"/> associated to failure dump item.
    /// </summary>
    /// <returns><see cref="Stream"/>.</returns>
    Task<Stream> GetStreamAsync();
}
