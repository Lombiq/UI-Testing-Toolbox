using Atata;
using Lombiq.Tests.UI.Components;

namespace Lombiq.Tests.UI.Pages
{
    public abstract class OrchardCoreAdminPage<TOwner> : Page<TOwner>
        where TOwner : OrchardCoreAdminPage<TOwner>
    {
        [FindByClass("menu-admin")]
        public Control<TOwner> AdminMenu { get; private set; }

        public OrchardCoreAdminTopNavbar<TOwner> TopNavbar { get; private set; }

        public TOwner ShouldStayOnAdminPage() =>
            AdminMenu.Should.Exist();

        public TOwner ShouldLeaveAdminPage() =>
            AdminMenu.Should.Not.Exist();

        protected override void OnVerify()
        {
            base.OnVerify();
            ShouldStayOnAdminPage();
        }
    }
}
