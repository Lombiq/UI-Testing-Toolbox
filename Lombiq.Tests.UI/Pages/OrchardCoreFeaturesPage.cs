using Atata;

namespace Lombiq.Tests.UI.Pages
{
    // Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
    using _ = OrchardCoreFeaturesPage;
#pragma warning restore IDE0065 // Misplaced using directive

    [Url("Admin/Features")]
    public sealed class OrchardCoreFeaturesPage : OrchardCoreAdminPage<_>
    {
        [FindById]
        public SearchInput<_> SearchBox { get; private set; }

        public ControlList<FeatureItem, _> Features { get; private set; }

        public _ EnsureFeatureIsEnabled(string featureName)
        {
            SearchBox.Set(featureName);

            var feature = Features[x => x.Name == featureName];

            if (!feature.IsEnabled)
            {
                feature.Enable.Click();

                AlertMessages.Should.Contain(x => x.IsSuccess && x.Text.Value.Contains(featureName));
            }

            return this;
        }

        [ControlDefinition("li", ContainingClass = "list-group-item")]
        public sealed class FeatureItem : Control<_>
        {
            [FindByXPath("h6", "label")]
            public Text<_> Name { get; private set; }

            [FindById(TermMatch.StartsWith, "btn-enable")]
            public Link<_> Enable { get; private set; }

            [FindById(TermMatch.StartsWith, "btn-disable")]
            public Link<_> Disable { get; private set; }

            protected override bool GetIsEnabled() =>
                !Enable.IsVisible;
        }
    }
}
