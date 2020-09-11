using Atata;
using Atata.Bootstrap;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Pages
{
    using _ = OrchardCoreSetupPage;

    [VerifyTitle("Orchard Setup", Format = "{0}")]
    [VerifyH1("Setup")]
    public class OrchardCoreSetupPage : Page<_>
    {
        [FindByName(nameof(SiteName))]
        public TextInput<_> SiteName { get; private set; }

        [FindById("recipeButton")]
        public RecipesDropdownToggle Recipe { get; private set; }

        [FindByName(nameof(UserName))]
        public TextInput<_> UserName { get; private set; }

        [FindByName(nameof(Email))]
        public EmailInput<_> Email { get; private set; }

        [FindByName(nameof(Password))]
        public PasswordInput<_> Password { get; private set; }

        [FindByName(nameof(PasswordConfirmation))]
        public PasswordInput<_> PasswordConfirmation { get; private set; }

        public Button<_> FinishSetup { get; private set; }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Only used by Atata.")]
        public class RecipesDropdownToggle : BSDropdownToggle<_> { }

        public _ SetupOrchardCore(
            string siteName = "Test Site",
            string recipeId = "SaaS",
            string userName = "admin",
            string email = "admin@admin.com",
            string password = "Password1!") =>
            SiteName
                .Set(siteName)
                .Recipe.Controls.CreateLink("TestRecipe", new FindByAttributeAttribute("data-recipe-name", recipeId)).Click()
                .UserName.Set(userName)
                .Email.Set(email)
                .Password.Set(password)
                .PasswordConfirmation.Set(password)
                .FinishSetup.Click();
    }
}
