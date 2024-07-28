using Atata;
using Atata.Bootstrap;
using Lombiq.Tests.UI.Components;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lombiq.Tests.UI.Pages;

// Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
using _ = OrchardCoreFeaturesPage;
#pragma warning restore IDE0065 // Misplaced using directive

[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
public sealed class OrchardCoreFeaturesPage : OrchardCoreAdminPage<_>
{
    [FindById]
    public SearchInput<_> SearchBox { get; private set; }

    [FindById("bulk-action-menu-button")]
    public BulkActionsDropdown BulkActions { get; private set; }

    public FeatureItemList Features { get; private set; }

    public FeatureItem SearchForFeature(string featureName) =>
        SearchBox.Set(featureName)
            .Features[featureName];

    public sealed class BulkActionsDropdown : BSDropdownToggle<_>
    {
        public Link<_> Enable { get; private set; }

        public Link<_> Disable { get; private set; }

        public Link<_> Toggle { get; private set; }
    }

    [ControlDefinition(
        "li[contains(@class, 'list-group-item') and not(contains(@class, 'd-none')) and .//label[contains(@class, 'form-check-label')]]",
        ComponentTypeName = "feature")]
    public sealed class FeatureItem : Control<_>
    {
        [FindFirst(Visibility = Visibility.Any)]
        [ClicksUsingActions]
        public CheckBox<_> CheckBox { get; private set; }

        [FindByXPath("label")]
        public Text<_> Name { get; private set; }

        [FindById(TermMatch.StartsWith, "btn-enable")]
        public Link<_> Enable { get; private set; }

        [FindById(TermMatch.StartsWith, "btn-disable")]
        [GoTemporarily]
        public Link<ConfirmationModal<_>, _> Disable { get; private set; }

        public _ DisableWithConfirmation() =>
            Disable.ClickAndGo()
                .Yes.ClickAndGo();

        protected override bool GetIsEnabled() => !Enable.IsVisible;
    }

    public sealed class FeatureItemList : ControlList<FeatureItem, _>
    {
        public FeatureItem this[string featureName] =>
            GetAll().First(item => item.Name.Content.Value.ContainsOrdinalIgnoreCase(featureName));
    }
}
