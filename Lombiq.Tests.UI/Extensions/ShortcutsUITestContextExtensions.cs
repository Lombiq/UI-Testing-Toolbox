using Atata;
using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.Shortcuts.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Entities;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.Modules.Manifest;
using OrchardCore.Recipes.Services;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Themes.Services;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Workflows.Http.Controllers;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Services;
using RestEase;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        var userNameContainer = context.Get(By.CssSelector("pre")).Text;
        return userNameContainer["UserName: ".Length..];
    }

    /// <summary>
    /// Sets the registration type in site settings.
    /// </summary>
    public static Task SetUserRegistrationTypeAsync(
        this UITestContext context,
        UserRegistrationType type,
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var siteService = shellScope.ServiceProvider.GetRequiredService<ISiteService>();
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
    /// If creating the user with the given parameters was not successfull.
    /// </exception>
    public static Task CreateUserAsync(
        this UITestContext context,
        string userName,
        string password,
        string email,
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var userService = shellScope.ServiceProvider.GetRequiredService<IUserService>();
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
                    exceptionLines.AppendLine("Create user error:");
                    errors.ForEach(entry =>
                        exceptionLines.AppendLine(CultureInfo.InvariantCulture, $"{entry.Key}: {entry.Value}"));
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
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var userManager = shellScope.ServiceProvider.GetRequiredService<UserManager<IUser>>();
                if ((await userManager.FindByNameAsync(userName)) is not User user)
                {
                    throw new UserNotFoundException($"{userName} not found!");
                }

                var roleManager = shellScope.ServiceProvider.GetRequiredService<RoleManager<IRole>>();
                if ((await roleManager.FindByNameAsync(roleManager.NormalizeKey(roleName))) is not Role role)
                {
                    throw new RoleNotFoundException($"{roleName} not found!");
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
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var roleManager = shellScope.ServiceProvider.GetRequiredService<RoleManager<IRole>>();
                if ((await roleManager.FindByNameAsync(roleManager.NormalizeKey(roleName))) is not Role role)
                {
                    throw new RoleNotFoundException($"{roleName} not found!");
                }

                var permissionClaim = role.RoleClaims.FirstOrDefault(roleClaim =>
                    roleClaim.ClaimType == Permission.ClaimType
                    && roleClaim.ClaimValue == permissionName);
                if (permissionClaim == null)
                {
                    var permissionProviders = shellScope.ServiceProvider.GetRequiredService<IEnumerable<IPermissionProvider>>();
                    if (!await PermissionExistsAsync(permissionProviders, permissionName))
                    {
                        throw new PermissionNotFoundException($"{permissionName} not found!");
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
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            shellScope =>
            {
                var shellFeatureManager = shellScope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
                var extensionManager = shellScope.ServiceProvider.GetRequiredService<IExtensionManager>();

                var feature = extensionManager.GetFeature(featureId);

                return shellFeatureManager.EnableFeaturesAsync(new[] { feature }, force: true);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Disables the feature with the given <paramref name="featureId"/> directly.
    /// </summary>
    public static Task DisableFeatureDirectlyAsync(
        this UITestContext context,
        string featureId,
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            shellScope =>
            {
                var shellFeatureManager = shellScope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
                var extensionManager = shellScope.ServiceProvider.GetRequiredService<IExtensionManager>();

                var feature = extensionManager.GetFeature(featureId);

                return shellFeatureManager.DisableFeaturesAsync(new[] { feature }, force: true);
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
        context.GetApi().GetApplicationInfoAsync();

    // This is required to instantiate ILogger<>.
    private sealed class ExecuteRecipeShortcut { }

    /// <summary>
    /// Executes a recipe identified by its name directly.
    /// </summary>
    /// <exception cref="RecipeNotFoundException">If no recipe found with the given <paramref name="recipeName"/>.</exception>
    public static Task ExecuteRecipeDirectlyAsync(
        this UITestContext context,
        string recipeName,
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var recipeHarvesters = shellScope.ServiceProvider.GetRequiredService<IEnumerable<IRecipeHarvester>>();
                var recipeCollections = await recipeHarvesters
                    .AwaitEachAsync(harvester => harvester.HarvestRecipesAsync());
                var recipe = recipeCollections
                    .SelectMany(recipeCollection => recipeCollection)
                    .SingleOrDefault(recipeDescriptor => recipeDescriptor.Name == recipeName);

                if (recipe == null)
                {
                    throw new RecipeNotFoundException($"{recipeName} not found!");
                }

                // Logic copied from OrchardCore.Recipes.Controllers.AdminController.
                var executionId = Guid.NewGuid().ToString("n");

                var environment = new Dictionary<string, object>();
                var logger = shellScope.ServiceProvider.GetRequiredService<ILogger<ExecuteRecipeShortcut>>();
                var recipeEnvironmentProviders = shellScope.ServiceProvider
                    .GetRequiredService<IEnumerable<IRecipeEnvironmentProvider>>();
                await recipeEnvironmentProviders
                    .OrderBy(environmentProvider => environmentProvider.Order)
                    .InvokeAsync((provider, env) => provider.PopulateEnvironmentAsync(env), environment, logger);

                var recipeExecutor = shellScope.ServiceProvider.GetRequiredService<IRecipeExecutor>();
                await recipeExecutor.ExecuteAsync(executionId, recipe, environment, CancellationToken.None);
            },
            tenant,
            activateShell);

    /// <summary>
    /// Navigates to a page whose action method throws <see cref="InvalidOperationException"/>. This causes ASP.NET Core
    /// to display an error page.
    /// </summary>
    public static Task GoToErrorPageDirectlyAsync(this UITestContext context) =>
        context.GoToAsync<ErrorController>(controller => controller.Index());

    private static IShortcutsApi GetApi(this UITestContext context) =>
        _apis.GetOrAdd(
            context.Scope.BaseUri.ToString(),
            _ =>
            {
                // To allow self-signed development certificates.

                var invalidCertificateAllowingHttpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    // Revoked certificates shouldn't be used though.
                    CheckCertificateRevocationList = true,
                };

                var httpClient = new HttpClient(invalidCertificateAllowingHttpClientHandler)
                {
                    BaseAddress = context.Scope.BaseUri,
                };

                return RestClient.For<IShortcutsApi>(httpClient);
            });

    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1600:Elements should be documented",
        Justification = "Just maps to controller actions.")]
    public interface IShortcutsApi
    {
        [Get("api/ApplicationInfo")]
        Task<ApplicationInfo> GetApplicationInfoAsync();
    }

    /// <summary>
    /// Selects theme by <paramref name="id"/>.
    /// </summary>
    /// <exception cref="ThemeNotFoundException">If no theme found with the given <paramref name="id"/>.</exception>
    public static Task SelectThemeAsync(
        this UITestContext context,
        string id,
        string tenant = "Default",
        bool activateShell = true) => context.Application
        .UsingScopeAsync(
            async shellScope =>
            {
                var shellFeatureManager = shellScope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
                var themeFeature = (await shellFeatureManager.GetAvailableFeaturesAsync())
                    .FirstOrDefault(feature => feature.IsTheme() && feature.Id == id);

                if (themeFeature == null)
                {
                    throw new ThemeNotFoundException($"{id} not found.");
                }

                if (IsAdminTheme(themeFeature.Extension.Manifest))
                {
                    var adminThemeService = shellScope.ServiceProvider.GetRequiredService<IAdminThemeService>();
                    await adminThemeService.SetAdminThemeAsync(id);
                }
                else
                {
                    var siteThemeService = shellScope.ServiceProvider.GetRequiredService<ISiteThemeService>();
                    await siteThemeService.SetSiteThemeAsync(id);
                }

                var enabledFeatures = await shellFeatureManager.GetEnabledFeaturesAsync();
                var isEnabled = enabledFeatures.Any(feature => feature.Extension.Id == themeFeature.Id);

                if (!isEnabled)
                {
                    await shellFeatureManager.EnableFeaturesAsync(new[] { themeFeature }, force: true);
                }
            },
            tenant,
            activateShell);

    /// <summary>
    /// Creates, sets up and navigates to a new URL prefixed tenant. Also changes <see cref="UITestContext.TenantName"/>.
    /// </summary>
    public static async Task CreateAndEnterTenantAsync(
        this UITestContext context,
        string name,
        string urlPrefix,
        string recipe,
        CreateTenant model = null)
    {
        model ??= new CreateTenant();

        await context.GoToAsync<TenantsController>(controller =>
            controller.Create(name, urlPrefix, recipe, model.ConnectionString, model.DatabaseProvider));

        await context.SetDropdownByValueAsync(By.Id("culturesList"), model.Language);
        await context.ClickAndFillInWithRetriesAsync(By.Id("SiteName"), name);
        if (!string.IsNullOrEmpty(model.TimeZone)) await context.SetDropdownByValueAsync(By.Id("SiteTimeZone"), model.TimeZone);

        await context.ClickAndFillInWithRetriesAsync(By.Id("UserName"), model.UserName);
        await context.ClickAndFillInWithRetriesAsync(By.Id("Email"), model.Email);
        await context.ClickAndFillInWithRetriesAsync(By.Id("Password"), model.Password);
        await context.ClickAndFillInWithRetriesAsync(By.Id("PasswordConfirmation"), model.Password);
        await context.ClickReliablyOnAsync(By.Id("SubmitButton"));

        context.TenantName = urlPrefix;
    }

    /// <summary>
    /// Retrieves URI for a <see cref="OrchardCore.Workflows.Http.Activities.HttpRequestEvent"/> in a workflow.
    /// </summary>
    /// <exception cref="WorkflowTypeNotFoundException">
    /// If no <see cref="OrchardCore.Workflows.Models.WorkflowType"/> found with the given <paramref name="workflowTypeId"/>.
    /// </exception>
    public static async Task<string> GenerateHttpEventUrlAsync(
        this UITestContext context,
        string workflowTypeId,
        string activityId,
        int tokenLifeSpan = 0,
        string tenant = "Default",
        bool activateShell = true)
    {
        string eventUrl = null;
        await context.Application
            .UsingScopeAsync(
                async shellScope =>
                {
                    var workflowTypeStore = shellScope.ServiceProvider.GetRequiredService<IWorkflowTypeStore>();

                    var workflowType = await workflowTypeStore.GetAsync(workflowTypeId);
                    if (workflowType == null)
                    {
                        throw new WorkflowTypeNotFoundException($"{workflowTypeId} not found!");
                    }

                    var securityTokenService = shellScope.ServiceProvider.GetRequiredService<ISecurityTokenService>();
                    var token = securityTokenService.CreateToken(
                        new WorkflowPayload(workflowType.WorkflowTypeId, activityId),
                        TimeSpan.FromDays(
                            tokenLifeSpan == 0 ? HttpWorkflowController.NoExpiryTokenLifespan : tokenLifeSpan));

                    // LinkGenerator.GetPathByAction(...) and UrlHelper.Action(...) not resolves url for
                    // HttpWorkflowController.Invoke action.
                    // https://github.com/OrchardCMS/OrchardCore/issues/11764.
                    eventUrl = $"/workflows/Invoke?token={Uri.EscapeDataString(token)}";
                },
                tenant,
                activateShell);

        return eventUrl;
    }

    private static bool IsAdminTheme(IManifestInfo manifest) =>
        manifest.Tags.Any(tag => tag.EqualsOrdinalIgnoreCase(ManifestConstants.AdminTag));

    private static async Task<bool> PermissionExistsAsync(
        IEnumerable<IPermissionProvider> permissionProviders, string permissionName) =>
        (await Task.WhenAll(permissionProviders.Select(provider => provider.GetPermissionsAsync())))
            .SelectMany(permissions => permissions)
            .Any(permission => permission.Name == permissionName);
}
