using Atata;
using Atata.Bootstrap;
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

        public _ SetupOrchardCore(
            string languageValue = "en",
            string siteName = "Test Site",
            string recipeId = "SaaS",
            string siteTimeZoneValue = null,
            DatabaseType databaseProvider = DatabaseType.Sqlite,
            string connectionString = null,
            string tablePrefix = null,
            string userName = "admin",
            string email = "admin@admin.com",
            string password = "Password1!")
        {
            var page = Language.Set(languageValue)
                .SiteName.Set(siteName)
                .Recipe.Controls.CreateLink("TestRecipe", new FindByAttributeAttribute("data-recipe-name", recipeId)).Click()
                .DatabaseProvider.Set(databaseProvider);

            if (!string.IsNullOrWhiteSpace(siteTimeZoneValue))
            {
                page.SiteTimeZone.Set(siteTimeZoneValue);
            }

            if (databaseProvider != DatabaseType.Sqlite)
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException(
                        nameof(databaseProvider),
                        "If the selected database provider is other than SQLite a connection string must be provided.");
                }

                if (!string.IsNullOrEmpty(tablePrefix)) page.TablePrefix.Set(tablePrefix);
                page.ConnectionString.Set(connectionString);
            }

            return page
                .UserName.Set(userName)
                .Email.Set(email)
                .Password.Set(password)
                .PasswordConfirmation.Set(password)
                .FinishSetup.Click();
        }
    }
}
