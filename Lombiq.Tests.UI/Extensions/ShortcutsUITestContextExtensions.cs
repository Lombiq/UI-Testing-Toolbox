using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.Shortcuts.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OrchardCore.Abstractions.Setup;
using OrchardCore.Admin;
using OrchardCore.Data;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Entities;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using OrchardCore.Modules.Manifest;
using OrchardCore.Recipes.Services;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Setup.Services;
using OrchardCore.Themes.Services;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Workflows.Http.Controllers;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using Refit;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

/// <summary>
/// Some useful shortcuts for test execution using the <c>Lombiq.Tests.UI.Shortcuts</c> module. Note that you have to
/// have it enabled in the app for these to work.
/// </summary>
public static class ShortcutsUITestContextExtensions
{
    public const string FeatureToggleTestBenchUrl = "/Lombiq.Tests.UI.Shortcuts/FeatureToggleTestBench/Index";

    private static readonly ConcurrentDictionary<string, IShortcutsApi> _apis = new();
    private static readonly SemaphoreSlim _recipeHarvesterSemaphore = new(1, 1);

    public static bool InteractiveModeHasBeenUsed { get; private set; }

    /// <summary>
    /// Authenticates the client with the given user account. Note that this will execute a direct sign in without
    /// anything else happening on the login page. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c>
    /// enabled.
    /// </summary>
    public static Task SignInDirectlyAsync(this UITestContext context, string userName = DefaultUser.UserName) =>
        context.GoToAsync<AccountController>(controller => controller.SignInDirectly(userName));

    /// <summary>
    /// Authenticates the client with the default user account and navigates to the given URL. Note that this will
    /// execute a direct sign in without anything else happening on the login page and going to a relative URL after
    /// login. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
    /// </summary>
    public static Task SignInDirectlyAndGoToRelativeUrlAsync(
        this UITestContext context,
        string relativeUrl,
        bool onlyIfNotAlreadyThere = true)
        => context.SignInDirectlyAndGoToRelativeUrlAsync(DefaultUser.UserName, relativeUrl, onlyIfNotAlreadyThere);

    /// <summary>
    /// Authenticates the client with the given user account and navigates to the given URL. Note that this will execute
    /// a direct sign in without anything else happening on the login page and going to a relative URL after login. The
    /// target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
    /// </summary>
    public static async Task SignInDirectlyAndGoToRelativeUrlAsync(
        this UITestContext context,
        string userName,
        string relativeUrl,
        bool onlyIfNotAlreadyThere = true)
    {
        await context.SignInDirectlyAsync(userName);
        await context.GoToRelativeUrlAsync(relativeUrl, onlyIfNotAlreadyThere);
    }

    /// <summary>
    /// Signs the client out. Note that this will execute a direct sign in without anything else happening on the logoff
    /// page. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
    /// </summary>
    public static Task SignOutDirectlyAsync(this UITestContext context) =>
        context.GoToAsync<AccountController>(controller => controller.SignOutDirectly());

    /// <summary>
    /// Retrieves the currently authenticated user's name, if any. The target app needs to have
    /// <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
    /// </summary>
    /// <returns>The currently authenticated user's name, empty or null string if the user is anonymous.</returns>
    public static async Task<string> GetCurrentUserNameAsync(this UITestContext context)
    {
        await context.GoToAsync<CurrentUserController>(controller => controller.Index());
        var userNameContainer = context.GetText(By.CssSelector("pre"));
        if (userNameContainer == "Unauthenticated") return string.Empty;
        return userNameContainer["UserName: ".Length..];
    }

    /// <summary>
    /// Sets the registration type in site settings.
    /// </summary>
    public static Task SetUserRegistrationTypeAsync(
        this UITestContext context,
        UserRegistrationType type,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var siteService = serviceProvider.GetRequiredService<ISiteService>();
                var settings = await siteService.LoadSiteSettingsAsync();

                settings.Alter<RegistrationSettings>(
                    nameof(RegistrationSettings),
                    registrationSettings => registrationSettings.UsersCanRegister = type);

                await siteService.UpdateSiteSettingsAsync(settings);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Creates a user with the given parameters.
    /// </summary>
    /// <exception cref="CreateUserFailedException">
    /// If creating the user with the given parameters was not successful.
    /// </exception>
    public static Task CreateUserAsync(
        this UITestContext context,
        string userName,
        string password,
        string email,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var userService = serviceProvider.GetRequiredService<IUserService>();
                var errors = new Dictionary<string, string>();
                var user = await userService.CreateUserAsync(
                    new User
                    {
                        UserName = userName,
                        Email = email,
                        EmailConfirmed = true,
                        IsEnabled = true,
                    },
                    password,
                    (key, error) => errors.Add(key, error));

                if (user == null)
                {
                    var exceptionLines = new StringBuilder();
                    exceptionLines.AppendLine("User creation error:");
                    errors.ForEach(entry =>
                        exceptionLines.AppendLine(CultureInfo.InvariantCulture, $"- {entry.Key}: {entry.Value}"));
                    throw new CreateUserFailedException(exceptionLines.ToString());
                }
            },
            tenant,
            activateShell);

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    /// <exception cref="RoleNotFoundException">If no role found with the given <paramref name="roleName"/>.</exception>
    /// <exception cref="UserNotFoundException">If no user found with the given <paramref name="userName"/>.</exception>
    public static Task AddUserToRoleAsync(
        this UITestContext context,
        string userName,
        string roleName,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<IUser>>();
                if ((await userManager.FindByNameAsync(userName)) is not User user)
                {
                    throw new UserNotFoundException($"User with the name \"{userName}\" not found.");
                }

                var roleManager = serviceProvider.GetRequiredService<RoleManager<IRole>>();
                if ((await roleManager.FindByNameAsync(roleManager.NormalizeKey(roleName))) is not Role role)
                {
                    throw new RoleNotFoundException($"Role with the name \"{roleName}\" not found.");
                }

                await userManager.AddToRoleAsync(user, role.NormalizedRoleName);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Adds a permission to a role.
    /// </summary>
    /// <exception cref="RoleNotFoundException">If no role found with the given <paramref name="roleName"/>.</exception>
    /// <exception cref="PermissionNotFoundException">
    /// If no permission found with the given <paramref name="permissionName"/>.
    /// </exception>
    public static Task AddPermissionToRoleAsync(
        this UITestContext context,
        string permissionName,
        string roleName,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IRole>>();
                if ((await roleManager.FindByNameAsync(roleManager.NormalizeKey(roleName))) is not Role role)
                {
                    throw new RoleNotFoundException($"Role with the name \"{roleName}\" not found.");
                }

                var permissionClaim = role.RoleClaims.Find(roleClaim =>
                    roleClaim.ClaimType == Permission.ClaimType
                    && roleClaim.ClaimValue == permissionName);
                if (permissionClaim == null)
                {
                    var permissionProviders = serviceProvider.GetRequiredService<IEnumerable<IPermissionProvider>>();
                    if (!await PermissionExistsAsync(permissionProviders, permissionName))
                    {
                        throw new PermissionNotFoundException($"Permission with the name \"{permissionName}\" not found.");
                    }

                    role.RoleClaims.Add(new() { ClaimType = Permission.ClaimType, ClaimValue = permissionName });

                    await roleManager.UpdateAsync(role);
                }
            },
            tenant,
            activateShell);

    /// <summary>
    /// Enables the feature with the given <paramref name="featureId"/> directly.
    /// </summary>
    public static Task EnableFeatureDirectlyAsync(
        this UITestContext context,
        string featureId,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            serviceProvider =>
            {
                var shellFeatureManager = serviceProvider.GetRequiredService<IShellFeaturesManager>();
                var extensionManager = serviceProvider.GetRequiredService<IExtensionManager>();

                var feature = extensionManager.GetFeature(featureId);

                return shellFeatureManager.EnableFeaturesAsync([feature], force: true);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Disables the feature with the given <paramref name="featureId"/> directly.
    /// </summary>
    public static Task DisableFeatureDirectlyAsync(
        this UITestContext context,
        string featureId,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            serviceProvider =>
            {
                var shellFeatureManager = serviceProvider.GetRequiredService<IShellFeaturesManager>();
                var extensionManager = serviceProvider.GetRequiredService<IExtensionManager>();

                var feature = extensionManager.GetFeature(featureId);

                return shellFeatureManager.DisableFeaturesAsync([feature], force: true);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Turns the <c>Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench</c> feature on, then off, and checks if the
    /// operations indeed worked. This can be used to test if anything breaks when a feature is enabled or disabled.
    /// </summary>
    public static async Task ExecuteAndAssertTestFeatureToggleAsync(this UITestContext context)
    {
        await context.EnableFeatureDirectlyAsync("Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench");
        await context.GoToRelativeUrlAsync(FeatureToggleTestBenchUrl);
        context.Scope.Driver.PageSource.ShouldContain("The Feature Toggle Test Bench worked.");
        await context.DisableFeatureDirectlyAsync("Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench");
        await context.GoToRelativeUrlAsync(FeatureToggleTestBenchUrl, onlyIfNotAlreadyThere: false);
        context.Scope.Driver.PageSource.ShouldNotContain("The Feature Toggle Test Bench worked.");
    }

    /// <summary>
    /// Purges the media cache without using any UI operations. Returns status code 500 in case of an error during cache
    /// clear.
    /// </summary>
    /// <param name="toggleTheFeature">
    /// In case the <c>Lombiq.Tests.UI.Shortcuts.MediaCachePurge</c> feature haven't been turned on yet, then set <see
    /// langword="true"/>.
    /// </param>
    public static async Task PurgeMediaCacheDirectlyAsync(this UITestContext context, bool toggleTheFeature = false)
    {
        if (toggleTheFeature)
        {
            await context.EnableFeatureDirectlyAsync("Lombiq.Tests.UI.Shortcuts.MediaCachePurge");
        }

        await context.GoToAsync<MediaCachePurgeController>(controller => controller.PurgeMediaCacheDirectly());

        if (toggleTheFeature)
        {
            await context.DisableFeatureDirectlyAsync("Lombiq.Tests.UI.Shortcuts.MediaCachePurge");
        }
    }

    /// <summary>
    /// Gets basic information about the Orchard Core application's executable. Also see the <see
    /// cref="ShortcutsConfiguration.InjectApplicationInfo"/> configuration for injecting the same data into the HTML
    /// output.
    /// </summary>
    /// <returns>Basic information about the Orchard Core application's executable.</returns>
    public static Task<ApplicationInfo> GetApplicationInfoAsync(this UITestContext context) =>
        context.GetApi().GetApplicationInfoFromApiAsync();

    private sealed class ExecuteRecipeShortcut { }

    /// <summary>
    /// Executes a recipe identified by its name directly.
    /// </summary>
    /// <exception cref="RecipeNotFoundException">
    /// If no recipe found with the given <paramref name="recipeName"/>.
    /// </exception>
    public static Task ExecuteRecipeDirectlyAsync(
        this UITestContext context,
        string recipeName,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                try
                {
                    await _recipeHarvesterSemaphore.WaitAsync();

                    var recipeHarvesters = serviceProvider.GetRequiredService<IEnumerable<IRecipeHarvester>>();
                    var recipeCollections = await recipeHarvesters
                        .AwaitEachAsync(harvester => harvester.HarvestRecipesAsync());
                    var recipe = recipeCollections
                        .SelectMany(recipeCollection => recipeCollection)
                        .SingleOrDefault(recipeDescriptor => recipeDescriptor.Name == recipeName)
                        ?? throw new RecipeNotFoundException($"Recipe with the name \"{recipeName}\" not found.");

                    // Logic copied from OrchardCore.Recipes.Controllers.AdminController.
                    var executionId = Guid.NewGuid().ToString("n");

                    var environment = new Dictionary<string, object>();
                    var logger = serviceProvider.GetRequiredService<ILogger<ExecuteRecipeShortcut>>();
                    var recipeEnvironmentProviders = serviceProvider
                        .GetRequiredService<IEnumerable<IRecipeEnvironmentProvider>>();
                    await recipeEnvironmentProviders
                        .OrderBy(environmentProvider => environmentProvider.Order)
                        .InvokeAsync((provider, env) => provider.PopulateEnvironmentAsync(env), environment, logger);

                    var recipeExecutor = serviceProvider.GetRequiredService<IRecipeExecutor>();
                    await recipeExecutor.ExecuteAsync(executionId, recipe, environment, CancellationToken.None);
                }
                finally
                {
                    _recipeHarvesterSemaphore.Release();
                }
            },
            tenant,
            activateShell);

    /// <summary>
    /// Navigates to a page whose action method throws <see cref="InvalidOperationException"/>. This causes ASP.NET Core
    /// to display an error page.
    /// </summary>
    public static Task GoToErrorPageDirectlyAsync(this UITestContext context) =>
        context.GoToAsync<ErrorController>(controller => controller.Index());

    private static IShortcutsApi GetApi(this UITestContext context)
    {
        // If there is a subdirectory-like URL prefix (e.g. for tenants) in the scope base URI, the requests will have
        // double slashes that results in 404 error. So the trailing slash has to be trimmed out.
        var baseUri = new Uri(context.Scope.BaseUri.ToString().TrimEnd('/'));

        return _apis.GetOrAdd(
            baseUri.AbsoluteUri,
            _ => RestService.For<IShortcutsApi>(HttpClientHelper.CreateCertificateIgnoringHttpClient(baseUri)));
    }

    /// <summary>
    /// A client interface for <c>Lombiq.Tests.UI.Shortcuts</c> web APIs.
    /// </summary>
    public interface IShortcutsApi
    {
        /// <summary>
        /// Sends a web request to <see cref="ApplicationInfoController.Get"/> endpoint.
        /// </summary>
        [Get("/api/ApplicationInfo")]
        Task<ApplicationInfo> GetApplicationInfoFromApiAsync();

        /// <summary>
        /// Sends a web request to <see cref="InteractiveModeController.IsInteractive"/> endpoint.
        /// </summary>
        [Get("/api/InteractiveMode/IsInteractive")]
        Task<bool> IsInteractiveModeEnabledAsync();
    }

    /// <summary>
    /// Selects theme by <paramref name="id"/> directly.
    /// </summary>
    /// <exception cref="ThemeNotFoundException">If no theme found with the given <paramref name="id"/>.</exception>
    [Obsolete("Use SetThemeDirectlyAsync() instead. This method will be removed in a future version.")]
    public static Task SelectThemeAsync(
        this UITestContext context,
        string id,
        string tenant = null,
        bool activateShell = true) =>
        context.SetThemeDirectlyAsync(id, tenant, activateShell);

    /// <summary>
    /// Sets the current site or admin theme by <paramref name="id"/> directly, activating the theme without user interaction.
    /// </summary>
    /// <exception cref="ThemeNotFoundException">
    /// Thrown if no theme was found with the given <paramref name="id"/>.
    /// </exception>
    public static Task SetThemeDirectlyAsync(
        this UITestContext context,
        string id,
        string tenant = null,
        bool activateShell = true) =>
        UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var shellFeatureManager = serviceProvider.GetRequiredService<IShellFeaturesManager>();
                var themeFeature = (await shellFeatureManager.GetAvailableFeaturesAsync())
                    .FirstOrDefault(feature => feature.IsTheme() && feature.Id == id)
                    ?? throw new ThemeNotFoundException($"Theme with the feature ID {id} not found.");

                if (IsAdminTheme(themeFeature.Extension.Manifest))
                {
                    var adminThemeService = serviceProvider.GetRequiredService<IAdminThemeService>();
                    await adminThemeService.SetAdminThemeAsync(id);
                }
                else
                {
                    var siteThemeService = serviceProvider.GetRequiredService<ISiteThemeService>();
                    await siteThemeService.SetSiteThemeAsync(id);
                }

                var enabledFeatures = await shellFeatureManager.GetEnabledFeaturesAsync();
                var isEnabled = enabledFeatures.Any(feature => feature.Extension.Id == themeFeature.Id);

                if (!isEnabled)
                {
                    await shellFeatureManager.EnableFeaturesAsync([themeFeature], force: true);
                }
            },
            tenant,
            activateShell);

    /// <summary>
    /// Creates, sets up, switches to (with <see cref="UITestContext.SwitchCurrentTenant(string, string)"/>), and
    /// navigates to a new URL-prefixed tenant.
    /// </summary>
    public static async Task CreateAndSwitchToTenantAsync(
        this UITestContext context,
        string name,
        string urlPrefix,
        OrchardCoreSetupParameters setupParameters)
    {
        setupParameters ??= new OrchardCoreSetupParameters(context);
        var databaseProvider = setupParameters.DatabaseProvider == OrchardCoreSetupPage.DatabaseType.SqlServer
            ? DatabaseProviderValue.SqlConnection
            : setupParameters.DatabaseProvider.ToString();

        await context.Application.UsingScopeAsync(
            async serviceProvider =>
            {
                var shellHost = serviceProvider.GetRequiredService<IShellHost>();
                if (shellHost.TryGetSettings(name, out _)) throw new InvalidOperationException("The tenant already exists.");

                var shellSettings = serviceProvider.GetRequiredService<IShellSettingsManager>().CreateDefaultSettings();

                shellSettings.Name = name;
                shellSettings.RequestUrlHost = string.Empty;
                shellSettings.RequestUrlPrefix = urlPrefix;
                shellSettings.State = TenantState.Uninitialized;

                shellSettings["RecipeName"] = setupParameters.RecipeId;

                await shellHost.UpdateShellSettingsAsync(shellSettings);
            });

        await context.Application.UsingScopeAsync(
            async serviceProvider =>
            {
                var setupService = serviceProvider.GetRequiredService<ISetupService>();

                var setupRecipes = await setupService.GetSetupRecipesAsync();
                var recipeDescriptor = setupRecipes.First(recipe => recipe.Name == setupParameters.RecipeId);
                var shellSettings = serviceProvider.GetRequiredService<IShellHost>().GetSettings(name);

                var setupContext = new SetupContext
                {
                    ShellSettings = shellSettings,
                    EnabledFeatures = null,
                    Errors = new Dictionary<string, string>(),
                    Recipe = recipeDescriptor,
                    Properties = new Dictionary<string, object>
                    {
                        { SetupConstants.SiteName, setupParameters.SiteName },
                        { SetupConstants.AdminUsername, setupParameters.UserName },
                        { SetupConstants.AdminEmail, setupParameters.Email },
                        { SetupConstants.AdminPassword, setupParameters.Password },
                        { SetupConstants.SiteTimeZone, setupParameters.SiteTimeZoneValue },
                        { SetupConstants.DatabaseProvider, databaseProvider },
                        { SetupConstants.DatabaseConnectionString, setupParameters.ConnectionString },
                        { SetupConstants.DatabaseTablePrefix, setupParameters.TablePrefix },
                    },
                };

                await setupService.SetupAsync(setupContext);
            });

        context.SwitchCurrentTenant(name, urlPrefix);
        await context.GoToRelativeUrlAsync("/");
    }

    /// <summary>
    /// Retrieves URL for a <see cref="OrchardCore.Workflows.Http.Activities.HttpRequestEvent"/> in a workflow.
    /// </summary>
    /// <exception cref="WorkflowTypeNotFoundException">
    /// If no <see cref="WorkflowType"/> found with the given <paramref name="workflowTypeId"/>.
    /// </exception>
    public static async Task<string> GenerateHttpEventUrlAsync(
        this UITestContext context,
        string workflowTypeId,
        string activityId,
        int tokenLifeSpan = 0,
        string tenant = null,
        bool activateShell = true)
    {
        string eventUrl = null;
        await UsingScopeAsync(
            context,
            async serviceProvider =>
            {
                var workflowTypeStore = serviceProvider.GetRequiredService<IWorkflowTypeStore>();

                var workflowType = await workflowTypeStore.GetAsync(workflowTypeId)
                    ?? throw new WorkflowTypeNotFoundException($"Workflow type with the ID {workflowTypeId} not found.");
                var securityTokenService = serviceProvider.GetRequiredService<ISecurityTokenService>();
                var token = securityTokenService.CreateToken(
                    new WorkflowPayload(workflowType.WorkflowTypeId, activityId),
                    TimeSpan.FromDays(
                        tokenLifeSpan == 0 ? HttpWorkflowController.NoExpiryTokenLifespan : tokenLifeSpan));

                // LinkGenerator.GetPathByAction(...) and UrlHelper.Action(...) doesn't resolve the URL for the
                // HttpWorkflowController.Invoke action since they rely on IActionContextAccessor.ActionContext.
                eventUrl = $"/workflows/Invoke?token={Uri.EscapeDataString(token)}";
            },
            tenant,
            activateShell);

        return eventUrl;
    }

    /// <summary>
    /// Retrieves the options of the given type from the current tenant's shell scope.
    /// </summary>
    public static async Task<IOptions<T>> GetTenantOptionsAsync<T>(
        this UITestContext context,
        string tenant = null,
        bool activateShell = true)
        where T : class
    {
        IOptions<T> options = null;
        await UsingScopeAsync(
            context,
            serviceProvider =>
            {
                options = serviceProvider.GetRequiredService<IOptions<T>>();
                return Task.CompletedTask;
            },
            tenant,
            activateShell);
        return options;
    }

    /// <summary>
    /// Switches to an interactive mode where control from the test is handed over and you can use the web app as an
    /// ordinary user from the browser or access its web APIs. To switch back to the test, click the button
    /// that'll be displayed in the browser, or open <see cref="InteractiveModeController.Continue"/>.
    /// </summary>
    /// <param name="notificationHtml">
    /// If not <see langword="null"/> or empty, an additional information notification is displayed with the provided
    /// HTML content.
    /// </param>
    public static async Task SwitchToInteractiveAsync(this UITestContext context, string notificationHtml = null)
    {
        InteractiveModeHasBeenUsed = true;
        await context.EnterInteractiveModeAsync(notificationHtml);
        await context.WaitInteractiveModeAsync();

        context.Driver.Close();
        context.SwitchToLastWindow();
    }

    /// <summary>
    /// Opens a new tab with the <see cref="InteractiveModeController"/> <see cref="InteractiveModeController.Index"/>
    /// page. Visiting this page enables the interactive mode flag so it can be awaited with the <see
    /// cref="WaitInteractiveModeAsync"/> extension method.
    /// </summary>
    internal static Task EnterInteractiveModeAsync(this UITestContext context, string notificationHtml)
    {
        context.Driver.SwitchTo().NewWindow(WindowType.Tab);
        context.Driver.SwitchTo().Window(context.Driver.WindowHandles[^1]);

        return context.GoToAsync<InteractiveModeController>(controller => controller.Index(notificationHtml));
    }

    /// <summary>
    /// Periodically polls the <see cref="IShortcutsApi.IsInteractiveModeEnabledAsync"/> and waits half a second if it's
    /// <see langword="true"/>.
    /// </summary>
    internal static async Task WaitInteractiveModeAsync(this UITestContext context)
    {
        var client = context.GetApi();
        while (await client.IsInteractiveModeEnabledAsync())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    private static bool IsAdminTheme(IManifestInfo manifest) =>
        manifest.Tags.Any(tag => tag.EqualsOrdinalIgnoreCase(value: ManifestConstants.AdminTag));

    private static async Task<bool> PermissionExistsAsync(
        IEnumerable<IPermissionProvider> permissionProviders,
        string permissionName)
    {
        var permissions = permissionProviders.ToAsyncEnumerable();
        await foreach (var provider in permissions)
        {
            var providerPermissions = await provider.GetPermissionsAsync();
            if (providerPermissions.Any(permission => permission.Name == permissionName))
                return true;
        }

        return false;
    }

    private static Task UsingScopeAsync(
        UITestContext context,
        Func<IServiceProvider, Task> execute,
        string tenant,
        bool activateShell) =>
        context.Application.UsingScopeAsync(execute, tenant ?? context.TenantName, activateShell);

    /// <summary>
    /// Places the provided <paramref name="steps"/> into a recipe and executes it with JSON Import.
    /// </summary>
    public static async Task ExecuteJsonRecipeAsync(this UITestContext context, params object[] steps)
    {
        await context.GoToAdminRelativeUrlAsync("/DeploymentPlan/Import/Json");

        var json = JsonSerializer.Serialize(new { steps });
        await context.FillInCodeMirrorEditorWithRetriesAsync(By.ClassName("CodeMirror"), json);
        await context.ClickReliablyOnAsync(By.CssSelector(".ta-content button[type='submit']"));
        context.ShouldBeSuccess();
    }

    /// <summary>
    /// Executes JSON Import in the admin menu with a single <c>settings</c> step containing the provided <paramref
    /// name="settingsContent"/> which may include multiple named site settings.
    /// </summary>
    public static Task ExecuteJsonRecipeSiteSettingsAsync(this UITestContext context, IDictionary<string, object> settingsContent)
    {
        settingsContent["name"] = "settings";
        return context.ExecuteJsonRecipeAsync(settingsContent);
    }

    /// <summary>
    /// Executes JSON Import in the admin menu with a single <c>settings</c> step containing the provided <paramref
    /// name="setting"/>.
    /// </summary>
    public static Task ExecuteJsonRecipeSiteSettingAsync<T>(this UITestContext context, T setting) =>
        context.ExecuteJsonRecipeSiteSettingsAsync(new Dictionary<string, object> { [typeof(T).Name] = setting });
}
