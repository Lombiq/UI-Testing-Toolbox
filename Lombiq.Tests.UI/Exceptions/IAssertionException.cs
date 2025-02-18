using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Exceptions;

/// <summary>
/// Marker interface for xUnit for assertion failure exceptions, see <see
/// href="https://xunit.net/docs/getting-started/v3/whats-new#:~:text=Third%20party%20assertion%20library%20extension%20points">
/// the xUnit docs</see>.
/// </summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "See above.")]
public interface IAssertionException
{
}
