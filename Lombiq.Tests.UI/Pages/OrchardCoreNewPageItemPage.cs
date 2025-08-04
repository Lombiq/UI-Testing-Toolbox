using Atata;
using System;

namespace Lombiq.Tests.UI.Pages;

[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public class OrchardCoreNewPageItemPage : OrchardCoreAdminPage<OrchardCoreNewPageItemPage>
{
    [FindByName("TitlePart.Title")]
    public TextInput<OrchardCoreNewPageItemPage> Title { get; private set; }

    [FindByName("submit.Publish")]
    public Button<OrchardCoreContentItemsPage, OrchardCoreNewPageItemPage> Publish { get; private set; }
}
