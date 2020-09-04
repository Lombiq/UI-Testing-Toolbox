using Atata;

namespace Lombiq.Tests.UI.Pages
{
    using _ = OrchardCoreDashboardPage;

    [ControlDefinition("div[contains(@class, 'menu-admin')]")]
    [Url("Admin")]
    public class OrchardCoreDashboardPage : Page<_>
    {
    }
}
