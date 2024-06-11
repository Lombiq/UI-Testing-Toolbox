using Atata;
using Atata.Bootstrap;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Pages;

// Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
using _ = OrchardCoreContentItemsPage;
#pragma warning restore IDE0065 // Misplaced using directive

[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
public class OrchardCoreContentItemsPage : OrchardCoreAdminPage<_>
{
    [FindById("new-dropdown")]
    public NewItemDropdown NewDropdown { get; private set; }

    public Link<_> NewPageLink { get; private set; }

    [FindById("items-form")]
    public UnorderedList<ContentListItem, _> Items { get; private set; }

    public OrchardCoreNewPageItemPage CreateNewPage() =>
        (NewPageLink.IsVisible ? NewPageLink : NewDropdown.Page)
            .ClickAndGo<OrchardCoreNewPageItemPage>();

    public sealed class NewItemDropdown : BSDropdownToggle<_>
    {
        public Link<_> Page { get; private set; }
    }

    [ControlDefinition("li[position() > 1]", ComponentTypeName = "item")]
    public sealed class ContentListItem : ListItem<_>
    {
        [FindByXPath("a")]
        public Text<_> Title { get; private set; }

        [FindByClass]
        public Link<_> View { get; private set; }
    }
}
