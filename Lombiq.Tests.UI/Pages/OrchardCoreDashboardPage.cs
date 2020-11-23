using Atata;

namespace Lombiq.Tests.UI.Pages
{
    // Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
    using _ = OrchardCoreDashboardPage;
#pragma warning restore IDE0065 // Misplaced using directive

    [Url("Admin")]
    public class OrchardCoreDashboardPage : Page<_>
    {
        [FindByClass("menu-admin")]
        public Content<_, _> AdminMenu { get; private set; }

        protected override void OnVerify()
        {
            base.OnVerify();
            AdminMenu.Should.Exist();
        }
    }
}
