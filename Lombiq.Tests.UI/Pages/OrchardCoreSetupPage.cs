using Atata;
using Atata.Bootstrap;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Pages
{
    // Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
    using _ = OrchardCoreSetupPage;
#pragma warning restore IDE0065 // Misplaced using directive

    [VerifyTitle("Orchard Setup", Format = "{0}")]
    [VerifyH1("Setup")]
    public sealed class OrchardCoreSetupPage : Page<_>
    {
        public enum DatabaseType
        {
            [Term("Sql Server")]
            SqlServer,
            Sqlite,
            MySql,
            Postgres,
        }

        [FindById("culturesList")]
        [SelectByValue]
        public Select<_> Language { get; private set; }

        [FindByName(nameof(SiteName))]
        public TextInput<_> SiteName { get; private set; }

        [FindById("recipeButton")]
        public BSDropdownToggle<_> Recipe { get; private set; }

        [FindById("SiteTimeZone")]
        [SelectByValue]
        public Select<_> SiteTimeZone { get; private set; }

        [FindById("DatabaseProvider")]
        public Select<DatabaseType, _> DatabaseProvider { get; private set; }

        [FindById("ConnectionString")]
        public PasswordInput<_> ConnectionString { get; private set; }

        [FindById("TablePrefix")]
        public TextInput<_> TablePrefix { get; private set; }

        [FindByName(nameof(UserName))]
        public TextInput<_> UserName { get; private set; }

        [FindByName(nameof(Email))]
        public EmailInput<_> Email { get; private set; }

        [FindByName(nameof(Password))]
        public PasswordInput<_> Password { get; private set; }

        [FindByName(nameof(PasswordConfirmation))]
        public PasswordInput<_> PasswordConfirmation { get; private set; }

        public Button<_> FinishSetup { get; private set; }

        public _ SetupOrchardCore(UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters();

            var page = Language.Set(parameters.LanguageValue)
                .SiteName.Set(parameters.SiteName)
                .Recipe.Controls.CreateLink("TestRecipe", new FindByAttributeAttribute("data-recipe-name", parameters.RecipeId)).Click()
                .DatabaseProvider.Set(parameters.DatabaseProvider);

            if (!string.IsNullOrWhiteSpace(parameters.SiteTimeZoneValue))
            {
                page.SiteTimeZone.Set(parameters.SiteTimeZoneValue);
            }

            if (parameters.DatabaseProvider != DatabaseType.Sqlite)
            {
                if (string.IsNullOrEmpty(parameters.ConnectionString))
                {
                    throw new InvalidOperationException(
                        $"{nameof(OrchardCoreSetupParameters)}.{nameof(parameters.DatabaseProvider)}: " +
                        "If the selected database provider is other than SQLite a connection string must be provided.");
                }

                if (!string.IsNullOrEmpty(parameters.TablePrefix)) page.TablePrefix.Set(parameters.TablePrefix);
                page.ConnectionString.Set(parameters.ConnectionString);
            }

            context.ClickAndFillInWithRetries(By.Name(nameof(Email)), parameters.Email);

            return page
                .UserName.Set(parameters.UserName)
                .Password.Set(parameters.Password)
                .PasswordConfirmation.Set(parameters.Password)
                .FinishSetup.Click();
        }
    }
}
