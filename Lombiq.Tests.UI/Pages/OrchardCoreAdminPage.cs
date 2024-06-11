using Atata;
using Lombiq.Tests.UI.Components;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Pages;

[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
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
