using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Pages
{
    public class OrchardCoreSetupConfiguration
    {
        public string LanguageValue { get; set; } = "en";
        public string SiteName { get; set; } = "Test Site";
        public string RecipeId { get; set; } = "SaaS";
        public string SiteTimeZoneValue { get; set; }
        public OrchardCoreSetupPage.DatabaseType DatabaseProvider { get; set; } = OrchardCoreSetupPage.DatabaseType.Sqlite;
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; }
        public string UserName { get; set; } = "admin";
        // On some platforms, probably due to keyboard settings, the @ character can be missing from the address when
        // entered into the textfield. The terminating Null fixes this, see: https://stackoverflow.com/a/52202594/220230.
        public string Email { get; set; } = "admin@admin.com" + Keys.Null;
        public string Password { get; set; } = "Password1!";
    }
}
