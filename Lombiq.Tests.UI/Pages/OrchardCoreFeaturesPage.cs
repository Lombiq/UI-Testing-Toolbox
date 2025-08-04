using Atata;
using Atata.Bootstrap;
using Lombiq.Tests.UI.Components;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lombiq.Tests.UI.Pages;

[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public sealed class OrchardCoreFeaturesPage : OrchardCoreAdminPage<OrchardCoreFeaturesPage>
{
    [FindById]
    public SearchInput<OrchardCoreFeaturesPage> SearchBox { get; private set; }

    [FindById("bulk-action-menu-button")]
    public BulkActionsDropdown BulkActions { get; private set; }

    public FeatureItemList Features { get; private set; }

    public FeatureItem SearchForFeature(string featureName) =>
        SearchBox.Set(featureName)
            .Features[featureName];

    public sealed class BulkActionsDropdown : BSDropdownToggle<OrchardCoreFeaturesPage>
    {
        public Link<OrchardCoreFeaturesPage> Enable { get; private set; }

        public Link<OrchardCoreFeaturesPage> Disable { get; private set; }

        public Link<OrchardCoreFeaturesPage> Toggle { get; private set; }
    }

    [ControlDefinition(
        "li[contains(@class, 'list-group-item') and not(contains(@class, 'd-none')) and .//label[contains(@class, 'form-check-label')]]",
        ComponentTypeName = "feature")]
    public sealed class FeatureItem : Control<OrchardCoreFeaturesPage>
    {
        [FindFirst(Visibility = Visibility.Any)]
        [ClicksUsingActions]
        public CheckBox<OrchardCoreFeaturesPage> CheckBox { get; private set; }

        [FindByXPath("label")]
        public Text<OrchardCoreFeaturesPage> Name { get; private set; }

        [FindById(TermMatch.StartsWith, "btn-enable")]
        public Link<OrchardCoreFeaturesPage> Enable { get; private set; }

        [FindById(TermMatch.StartsWith, "btn-disable")]
        [GoTemporarily]
        public Link<ConfirmationModal<OrchardCoreFeaturesPage>, OrchardCoreFeaturesPage> Disable { get; private set; }

        public OrchardCoreFeaturesPage DisableWithConfirmation() =>
            Disable.ClickAndGo()
                .Yes.ClickAndGo();

        protected override bool GetIsEnabled() => !Enable.IsVisible;
    }

    public sealed class FeatureItemList : ControlList<FeatureItem, OrchardCoreFeaturesPage>
    {
        public FeatureItem this[string featureName] =>
            GetAll().First(item => item.Name.Content.Value.ContainsOrdinalIgnoreCase(featureName));
    }
}
