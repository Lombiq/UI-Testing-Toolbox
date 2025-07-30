using Atata;
using Atata.Bootstrap;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Pages;

[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public class OrchardCoreContentItemsPage : OrchardCoreAdminPage<OrchardCoreContentItemsPage>
{
    [FindById("new-dropdown")]
    public NewItemDropdown NewDropdown { get; private set; }

    public Link<OrchardCoreContentItemsPage> NewPageLink { get; private set; }

    [FindById("items-form")]
    public UnorderedList<ContentListItem, OrchardCoreContentItemsPage> Items { get; private set; }

    public OrchardCoreNewPageItemPage CreateNewPage() =>
        (NewPageLink.IsVisible ? NewPageLink : NewDropdown.Page)
            .ClickAndGo<OrchardCoreNewPageItemPage>();

    public sealed class NewItemDropdown : BSDropdownToggle<OrchardCoreContentItemsPage>
    {
        public Link<OrchardCoreContentItemsPage> Page { get; private set; }
    }

    [ControlDefinition("li[position() > 1]", ComponentTypeName = "item")]
    public sealed class ContentListItem : ListItem<OrchardCoreContentItemsPage>
    {
        [FindByXPath("a")]
        public Text<OrchardCoreContentItemsPage> Title { get; private set; }

        [FindByClass]
        public Link<OrchardCoreContentItemsPage> View { get; private set; }
    }
}
