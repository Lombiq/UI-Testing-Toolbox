using Atata;

namespace Lombiq.Tests.UI.Components;

[ControlDefinition("ul", ContainingClass = "menu-admin", ComponentTypeName = "menu", IgnoreNameEndings = "Menu")]
public sealed class OrchardCoreAdminMenu<TOwner> : HierarchicalUnorderedList<OrchardCoreAdminMenu<TOwner>.MenuItem, TOwner>
    where TOwner : PageObject<TOwner>
{
    public MenuItem FindMenuItem(string menuItemName) =>
        Descendants.GetByXPathCondition(menuItemName, $"{MenuItem.XPathTo.Title}[.='{menuItemName}']");

    [ControlDefinition("li", ComponentTypeName = "menu item", Visibility = Visibility.Any)]
    [FindSettings(Visibility = Visibility.Any, TargetAllChildren = true)]
    public sealed class MenuItem : HierarchicalListItem<MenuItem, TOwner>
    {
        [FindByXPath(XPathTo.Title)]
        [GetsContentFromSource(ContentSource.TextContent)]
        public Text<TOwner> Title { get; private set; }

        internal static class XPathTo
        {
            internal const string Title = "a/span[contains(concat(' ', normalize-space(@class), ' '), ' title ')]";
        }
    }
}
