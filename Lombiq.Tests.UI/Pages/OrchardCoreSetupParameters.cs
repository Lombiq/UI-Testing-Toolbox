using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Pages;

public class OrchardCoreSetupParameters
{
    private UserRegistrationParameters _userRegistrationParameters;

    public string LanguageValue { get; set; } = "en";
    public string SiteName { get; set; } = "Test Site";
    public string RecipeId { get; set; } = "SaaS";
    public string SiteTimeZoneValue { get; set; }
    public OrchardCoreSetupPage.DatabaseType DatabaseProvider { get; set; } = OrchardCoreSetupPage.DatabaseType.Sqlite;
    public string ConnectionString { get; set; }
    public string TablePrefix { get; set; }
    public string UserName { get; set; } = DefaultUser.UserName;
    public string Email { get; set; } = DefaultUser.Email;
    public string Password { get; set; } = DefaultUser.Password;
    public bool RunSetupOnCurrentPage { get; set; }
    public bool SkipSetup { get; set; }
    public bool SkipRegistration { get; set; }
    public bool SkipFrontend { get; set; }

    /// <summary>
    /// Gets or sets the value of the parameters used for registration and login for a valid test user. By default, it
    /// gets the <see cref="TestUser"/>, and it's reset to that value if set to <see langword="null"/>.
    /// </summary>
    public UserRegistrationParameters UserRegistrationParameters
    {
        get => _userRegistrationParameters ??
               (SkipRegistration ? UserRegistrationParameters.CreateTest() : new(UserName, Email, Password));
        set => _userRegistrationParameters = value;
    }

    public OrchardCoreSetupParameters(UITestContext context = null, string recipeId = null)
    {
        if (context != null)
        {
            DatabaseProvider = context.Configuration.UseSqlServer
                ? OrchardCoreSetupPage.DatabaseType.SqlServer
                : OrchardCoreSetupPage.DatabaseType.Sqlite;

            ConnectionString = context.Configuration.UseSqlServer
                ? context.SqlServerRunningContext.ConnectionString
                : null;
        }

        if (!string.IsNullOrEmpty(recipeId)) RecipeId = recipeId;
    }
}
