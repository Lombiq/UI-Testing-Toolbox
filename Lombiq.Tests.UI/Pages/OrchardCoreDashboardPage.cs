using Atata;

namespace Lombiq.Tests.UI.Pages
{
    using _ = OrchardCoreDashboardPage;

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
