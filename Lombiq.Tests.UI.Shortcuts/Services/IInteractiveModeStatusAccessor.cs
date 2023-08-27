namespace Lombiq.Tests.UI.Shortcuts.Services;

/// <summary>
/// A container for the flag indicating if interactive mode is enabled.
/// </summary>
public interface IInteractiveModeStatusAccessor
{
    /// <summary>
    /// Gets or sets a value indicating whether interactive mode is enabled.
    /// </summary>
    bool Enabled { get; set; }
}
