using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Pages;

public class OrchardCoreSetupParameters
{
    private UserRegistrationParameters _userRegistrationParameters;
    private UserRegistrationParameters _userLoginParameters;

    public static By FinishSetupSelector { get; } = By.XPath("id('SubmitButton')[contains(., 'Finish Setup')]");

    public string LanguageValue { get; set; } = "en";
    public string SiteName { get; set; } = "Test Site";
    public string RecipeId { get; set; } = "SaaS";
    public string SiteTimeZoneValue { get; set; }
    public DatabaseType DatabaseProvider { get; set; }
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
    /// Gets or sets the value of the parameters used for registration of a valid test user. By default, it gets the
    /// <see cref="TestUser"/>, and it's reset to that value if set to <see langword="null"/>.
    /// </summary>
    public UserRegistrationParameters UserRegistrationParameters
    {
        get => _userRegistrationParameters ?? UserRegistrationParameters.CreateTest();
        set => _userRegistrationParameters = value;
    }

    /// <summary>
    /// Gets or sets the value of the parameters used for login with a valid test user. By default, it gets the <see
    /// cref="DefaultUser"/>, and it's reset to that value if set to <see langword="null"/>.
    /// </summary>
    public UserRegistrationParameters UserLoginParameters
    {
        get => _userLoginParameters ?? UserRegistrationParameters.CreateDefaultUser();
        set => _userLoginParameters = value;
    }

    public OrchardCoreSetupParameters(UITestContext context = null, string recipeId = null)
    {
        if (context != null)
        {
            DatabaseProvider = context.Configuration.UseSqlServer
                ? DatabaseType.SqlServer
                : DatabaseType.Sqlite;

            ConnectionString = context.Configuration.UseSqlServer
                ? context.SqlServerRunningContext.ConnectionString
                : null;
        }

        if (!string.IsNullOrEmpty(recipeId)) RecipeId = recipeId;
    }

    public async Task SetupOrchardCoreAsync(UITestContext context)
    {
        await context.SetDropdownByValueAsync(By.Id("culturesList"), LanguageValue);
        await context.ClickAndFillInWithRetriesAsync(By.Id("SiteName"), SiteName);

        if (!string.IsNullOrEmpty(RecipeId))
        {
            // If there are a lot of recipes and "headless" mode is disabled, the recipe can become unclickable because
            // the list of recipes is too long, and it's off the screen. So we need to use JavaScript for clicking it.
            context.ExecuteScript(
                $"document.querySelector('a[data-recipe-name={JsonSerializer.Serialize(RecipeId)}]').click()");
        }

        if (DatabaseProvider != DatabaseType.ProvidedByEnvironment)
        {
            await context.SetDropdownByValueAsync(By.Id("DatabaseProvider"), DatabaseProvider.ToString());
        }

        if (!string.IsNullOrWhiteSpace(SiteTimeZoneValue))
        {
            await context.SetDropdownByValueAsync(By.Id("SiteTimeZone"), SiteTimeZoneValue);
        }

        if (DatabaseProvider is not DatabaseType.Sqlite and not DatabaseType.ProvidedByEnvironment)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException(
                    $"{nameof(OrchardCoreSetupParameters)}.{nameof(DatabaseProvider)}: " +
                    "If the selected database provider is other than SQLite, a connection string must be provided.");
            }

            await context.ClickAndFillInWithRetriesAsync(By.Id("TablePrefix"), TablePrefix);
            await context.ClickAndFillInWithRetriesAsync(By.Id("ConnectionString"), ConnectionString);
        }

        await context.ClickAndFillInWithRetriesAsync(By.Id("UserName"), UserName);
        await context.ClickAndFillInWithRetriesAsync(By.Id("Email"), Email);
        await context.ClickAndFillInWithRetriesAsync(By.Id("Password"), Password);
        await context.ClickAndFillInWithRetriesAsync(By.Id("PasswordConfirmation"), Password);

        await context.ClickReliablyOnAsync(FinishSetupSelector);

        await context.TriggerAfterPageChangeEventAndRefreshAtataContextAsync();
    }

    public enum DatabaseType
    {
        Sqlite,
        SqlServer,
        MySql,
        Postgres,
        ProvidedByEnvironment,
    }
}
