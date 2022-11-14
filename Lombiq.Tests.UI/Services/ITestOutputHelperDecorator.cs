using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Defines an <see cref="ITestOutputHelper"/> that decorates another <see cref="ITestOutputHelper"/>.
/// </summary>
public interface ITestOutputHelperDecorator : ITestOutputHelper
{
    /// <summary>
    /// Gets the decorated <see cref="ITestOutputHelper"/> instance.
    /// </summary>
    ITestOutputHelper Decorated { get; }
}
