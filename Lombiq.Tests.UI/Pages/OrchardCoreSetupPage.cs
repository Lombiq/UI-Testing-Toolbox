using Atata;
using Atata.Bootstrap;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Pages
{
    using _ = OrchardCoreSetupPage;

    [VerifyTitle("Orchard Setup", Format = "{0}")]
    [VerifyH1("Setup")]
    public class OrchardCoreSetupPage : Page<_>
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

        public _ SetupOrchardCore(UITestContext context, OrchardCoreSetupConfiguration configuration = null)
        {
            configuration ??= new OrchardCoreSetupConfiguration();

            var page = Language.Set(configuration.LanguageValue)
                .SiteName.Set(configuration.SiteName)
                .Recipe.Controls.CreateLink("TestRecipe", new FindByAttributeAttribute("data-recipe-name", configuration.RecipeId)).Click()
                .DatabaseProvider.Set(configuration.DatabaseProvider);

            if (!string.IsNullOrWhiteSpace(configuration.SiteTimeZoneValue))
            {
                page.SiteTimeZone.Set(configuration.SiteTimeZoneValue);
            }

            if (configuration.DatabaseProvider != DatabaseType.Sqlite)
            {
                if (string.IsNullOrEmpty(configuration.ConnectionString))
                {
                    throw new InvalidOperationException(
                        $"{nameof(OrchardCoreSetupConfiguration)}.{nameof(configuration.DatabaseProvider)}: " +
                        "If the selected database provider is other than SQLite a connection string must be provided.");
                }

                if (!string.IsNullOrEmpty(configuration.TablePrefix)) page.TablePrefix.Set(configuration.TablePrefix);
                page.ConnectionString.Set(configuration.ConnectionString);
            }

            context.Get(By.Name(nameof(Email))).ClickAndFillInWithRetries(configuration.Email, context);

            return page
                .UserName.Set(configuration.UserName)
                .Password.Set(configuration.Password)
                .PasswordConfirmation.Set(configuration.Password)
                .FinishSetup.Click();
        }
    }
}
