using Atata;
using Atata.Bootstrap;
using Lombiq.Tests.UI.Attributes.Behaviors;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Pages;

[VerifyTitle(values: [DefaultPageTitle, OlderPageTitle], Format = "{0}")]
[VerifyH1(DefaultPageTitle, OlderPageTitle)]
[TermFindSettings(
    Case = TermCase.Pascal,
    TargetAllChildren = true,
    TargetAttributeTypes = [typeof(FindByIdAttribute), typeof(FindByNameAttribute)])]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public sealed class OrchardCoreSetupPage : Page<OrchardCoreSetupPage>
{
    public const string DefaultPageTitle = "Setup";
    public const string OlderPageTitle = "Orchard Setup";

    public enum DatabaseType
    {
        [Term("Sql Server")]
        SqlServer,
        Sqlite,
        [Term("MySql")]
        MySql,
        Postgres,
        ProvidedByEnvironment,
    }

    [FindById("culturesList")]
    [SelectsOptionByValue]
    public Select<OrchardCoreSetupPage> Language { get; private set; }

    [FindByName]
    public TextInput<OrchardCoreSetupPage> SiteName { get; private set; }

    [FindById("recipeButton")]
    public BSDropdownToggle<OrchardCoreSetupPage> Recipe { get; private set; }

    [FindById]
    [SelectsOptionByValue]
    public Select<OrchardCoreSetupPage> SiteTimeZone { get; private set; }

    [FindById]
    public Select<DatabaseType, OrchardCoreSetupPage> DatabaseProvider { get; private set; }

    [FindById]
    public PasswordInput<OrchardCoreSetupPage> ConnectionString { get; private set; }

    [FindById]
    public TextInput<OrchardCoreSetupPage> TablePrefix { get; private set; }

    [FindByName]
    public TextInput<OrchardCoreSetupPage> UserName { get; private set; }

    [FindByName]
    [SetsValueReliably]
    public EmailInput<OrchardCoreSetupPage> Email { get; private set; }

    [FindByName]
    public PasswordInput<OrchardCoreSetupPage> Password { get; private set; }

    [FindByName]
    public PasswordInput<OrchardCoreSetupPage> PasswordConfirmation { get; private set; }

    public Button<OrchardCoreSetupPage> FinishSetup { get; private set; }

    public OrchardCoreSetupPage ShouldStayOnSetupPage() => PageTitle.Should.Satisfy(title => IsExpectedTitle(title));

    public OrchardCoreSetupPage ShouldLeaveSetupPage() => PageTitle.Should.Not.Satisfy(title => IsExpectedTitle(title));

    public OrchardCoreSetupPage ShouldLeaveSetupPage(bool expected) =>
        expected ? ShouldLeaveSetupPage() : ShouldStayOnSetupPage();

    public async Task<OrchardCoreSetupPage> SetupOrchardCoreAsync(UITestContext context, OrchardCoreSetupParameters parameters = null)
    {
        parameters ??= new OrchardCoreSetupParameters(context);

        Language.Set(parameters.LanguageValue);
        SiteName.Set(parameters.SiteName);

        if (!string.IsNullOrEmpty(parameters.RecipeId))
        {
            // If there are a lot of recipes and "headless" mode is disabled, the recipe can become unclickable because
            // the list of recipes is too long and it's off the screen, so we need to use JavaScript for clicking it.
            context
                .ExecuteScript("document.querySelectorAll(\"a[data-recipe-name='" + parameters.RecipeId + "']\")[0]" +
                ".click()");
        }

        if (parameters.DatabaseProvider != DatabaseType.ProvidedByEnvironment)
        {
            DatabaseProvider.Set(parameters.DatabaseProvider);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SiteTimeZoneValue))
        {
            SiteTimeZone.Set(parameters.SiteTimeZoneValue);
        }

        if (parameters.DatabaseProvider is not DatabaseType.Sqlite and not DatabaseType.ProvidedByEnvironment)
        {
            if (string.IsNullOrEmpty(parameters.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"{nameof(OrchardCoreSetupParameters)}.{nameof(parameters.DatabaseProvider)}: " +
                    "If the selected database provider is other than SQLite, a connection string must be provided.");
            }

            if (!string.IsNullOrEmpty(parameters.TablePrefix)) TablePrefix.Set(parameters.TablePrefix);
            ConnectionString.Set(parameters.ConnectionString);
        }

        Email.Set(parameters.Email);
        UserName.Set(parameters.UserName);
        Password.Set(parameters.Password);
        PasswordConfirmation.Set(parameters.Password);

        FinishSetup.Click();

        await context.TriggerAfterPageChangeEventAndRefreshAtataContextAsync();

        context.RefreshCurrentAtataContext();

        return this;
    }

    private static bool IsExpectedTitle(string title) =>
        title.EqualsOrdinalIgnoreCase(DefaultPageTitle) || title.EqualsOrdinalIgnoreCase(OlderPageTitle);
}
