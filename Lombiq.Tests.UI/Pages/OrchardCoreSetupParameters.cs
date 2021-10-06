using Lombiq.Tests.UI.Constants;

namespace Lombiq.Tests.UI.Pages
{
    public class OrchardCoreSetupParameters
    {
        public string LanguageValue { get; set; } = "en";
        public string SiteName { get; set; } = "Test Site";
        public string RecipeId { get; set; } = "SaaS";
        public string SiteTimeZoneValue { get; set; }
        public OrchardCoreSetupPage.DatabaseType DatabaseProvider { get; set; } = OrchardCoreSetupPage.DatabaseType.Sqlite;
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; }
        public string UserName { get; set; } = DefaultUser.UserName;
        public string Email { get; set; } = DefaultUser.UserName + "@admin.com";
        public string Password { get; set; } = DefaultUser.Password;
    }
}
