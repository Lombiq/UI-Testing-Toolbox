using Lombiq.Tests.UI.Extensions;

namespace Lombiq.Tests.UI.Services;

public class ShortcutsConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to inject a comment into the site's HTML output with basic information
    /// about the Orchard Core application's executable. Also see <see
    /// cref="ShortcutsUITestContextExtensions.GetApplicationInfoAsync"/> for a shortcut for retrieving the same data
    /// on-demand.
    /// </summary>
    public bool InjectApplicationInfo { get; set; }
}
