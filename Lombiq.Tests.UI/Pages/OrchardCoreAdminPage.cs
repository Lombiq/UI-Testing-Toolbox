using Atata;
using Lombiq.Tests.UI.Components;

namespace Lombiq.Tests.UI.Pages;

public abstract class OrchardCoreAdminPage<TOwner> : Page<TOwner>
    where TOwner : OrchardCoreAdminPage<TOwner>
{
    public OrchardCoreAdminTopNavbar<TOwner> TopNavbar { get; private set; }

    public OrchardCoreAdminMenu<TOwner> AdminMenu { get; private set; }

    public ControlList<AlertMessage<TOwner>, TOwner> AlertMessages { get; private set; }

    public TOwner ShouldStayOnAdminPage() => AdminMenu.Should.BePresent();

    public TOwner ShouldLeaveAdminPage() => AdminMenu.Should.Not.BePresent();

    protected override void OnVerify()
    {
        base.OnVerify();
        ShouldStayOnAdminPage();
    }

    public TOwner ShouldContainSuccessAlertMessage(TermMatch expectedMatch, string expectedText) =>
        AlertMessages.Should.Contain(message => message.IsSuccess && expectedMatch.IsMatch(message.Text.Value, expectedText));
}
