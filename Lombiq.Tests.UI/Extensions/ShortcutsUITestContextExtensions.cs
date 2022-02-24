using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.Shortcuts.Models;
using OpenQA.Selenium;
using RestEase;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Some useful shortcuts for test execution using the <c>Lombiq.Tests.UI.Shortcuts</c> module. Note that you have
    /// to have it enabled in the app for these to work.
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
        /// Authenticates the client with the given user account and navigates to the given URL. Note that this will
        /// execute a direct sign in without anything else happening on the login page and going to a relative URL after
        /// login. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
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
        /// Signs the client out. Note that this will execute a direct sign in without anything else happening on the
        /// logoff page. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
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
        /// Enables the feature with the given ID directly, without anything
        /// else happening on the admin Features page. The target app needs to
        /// have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static Task EnableFeatureDirectlyAsync(this UITestContext context, string featureId) =>
            context.GoToAsync<ShellFeaturesController>(controller => controller.EnableFeatureDirectly(featureId));

        /// <summary>
        /// Disables the feature with the given ID directly, without anything else happening on the admin Features page.
        /// The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static Task DisableFeatureDirectlyAsync(this UITestContext context, string featureId) =>
            context.GoToAsync<ShellFeaturesController>(controller => controller.DisableFeatureDirectly(featureId));

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
            await context.GoToRelativeUrlAsync(FeatureToggleTestBenchUrl);
            context.Scope.Driver.PageSource.ShouldNotContain("The Feature Toggle Test Bench worked.");
        }

        /// <summary>
        /// Purges the media cache without using any UI operations. Returns status code 500 in case of an error during
        /// cache clear.
        /// </summary>
        /// <param name="toggleTheFeature">
        /// In case the <c>Lombiq.Tests.UI.Shortcuts.MediaCachePurge</c> feature haven't been turned on yet, then set
        /// <see langword="true"/>.
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
        /// cref="ShortcutsConfiguration.InjectApplicationInfo"/> configuration for injecting the same data into the
        /// HTML output.
        /// </summary>
        /// <returns>Basic information about the Orchard Core application's executable.</returns>
        public static Task<ApplicationInfo> GetApplicationInfoAsync(this UITestContext context) =>
            context.GetApi().GetApplicationInfoAsync();

        /// <summary>
        /// Executes a recipe identified by its name directly. The user must be logged in. The target app needs to have
        /// <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static Task ExecuteRecipeDirectlyAsync(this UITestContext context, string recipeName) =>
            context.GoToAsync<RecipeController>(controller => controller.Execute(recipeName));

        /// <summary>
        /// Navigates to a page whose action method throws <see cref="InvalidOperationException"/>. This causes ASP.NET
        /// Core to display an error page.
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
    }
}
