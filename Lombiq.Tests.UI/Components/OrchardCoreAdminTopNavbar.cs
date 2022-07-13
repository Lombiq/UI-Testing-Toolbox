using Atata;
using Atata.Bootstrap;

namespace Lombiq.Tests.UI.Components;

[ControlDefinition(ContainingClass = "ta-navbar-top", ComponentTypeName = "navbar")]
public class OrchardCoreAdminTopNavbar<TOwner> : Control<TOwner>
    where TOwner : PageObject<TOwner>
{
    [FindFirst]
    public AccountDropdown Account { get; private set; }

    public class AccountDropdown : BSDropdown<TOwner>
    {
        [FindByContent(TermMatch.Contains, TermCase.Sentence)]
        public Button<TOwner> LogOff { get; private set; }
    }
}
