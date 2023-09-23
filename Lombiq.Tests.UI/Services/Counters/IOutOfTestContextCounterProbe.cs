namespace Lombiq.Tests.UI.Services.Counters;

/// <summary>
/// Represents a probe that is out of the test context, i.e. it's not executed in the test context, but in the web
/// application context.
/// </summary>
public interface IOutOfTestContextCounterProbe : ICounterProbe
{
}
