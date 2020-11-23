using Atata;

namespace Lombiq.Tests.UI.Pages
{
    // Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
    using _ = CurrentUserPage;
#pragma warning restore IDE0065 // Misplaced using directive

    [Url("Lombiq.Tests.UI.Shortcuts/CurrentUser/Index")]
    public class CurrentUserPage : Page<_>
    {
        // The action returns just a string which will be wrapped into a document and pre tag in the page source.
        [FindByXPath("//pre", Visibility = Visibility.Visible)]
        public Text<_> LoggedInUser { get; private set; }
    }
}
