using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.Shortcuts.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OrchardCore.Mvc.Core.Utilities;
using RestEase;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
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
        private const string AreaUrl = "/Lombiq.Tests.UI.Shortcuts/";

        public const string FeatureToggleTestBenchUrl = AreaUrl + "FeatureToggleTestBench/Index";

        private static readonly ConcurrentDictionary<string, IShortcutsApi> _apis = new();

        /// <summary>
        /// Authenticates the client with the given user account. Note that this will execute a direct sign in without
        /// anything else happening on the login page. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c>
        /// enabled.
        /// </summary>
        public static void SignInDirectly(this UITestContext context, string userName = DefaultUser.UserName) =>
                GoTo<AccountController>(context, nameof(AccountController.SignInDirectly), (nameof(userName), userName));

        /// <summary>
        /// Authenticates the client with the default user account and navigates to the given URL. Note that this will
        /// execute a direct sign in without anything else happening on the login page and going to a relative URL after
        /// login. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static void SignInDirectlyAndGoToRelativeUrl(
            this UITestContext context,
            string relativeUrl,
            bool onlyIfNotAlreadyThere = true)
            => context.SignInDirectlyAndGoToRelativeUrl(DefaultUser.UserName, relativeUrl, onlyIfNotAlreadyThere);

        /// <summary>
        /// Authenticates the client with the given user account and navigates to the given URL. Note that this will
        /// execute a direct sign in without anything else happening on the login page and going to a relative URL after
        /// login. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static void SignInDirectlyAndGoToRelativeUrl(
            this UITestContext context,
            string userName,
            string relativeUrl,
            bool onlyIfNotAlreadyThere = true)
        {
            context.SignInDirectly(userName);
            context.GoToRelativeUrl(relativeUrl, onlyIfNotAlreadyThere);
        }

        /// <summary>
        /// Signs the client out. Note that this will execute a direct sign in without anything else happening on the
        /// logoff page. The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static void SignOutDirectly(this UITestContext context) =>
            GoTo<AccountController>(context, nameof(SignOutDirectly));

        /// <summary>
        /// Retrieves the currently authenticated user's name, if any. The target app needs to have
        /// <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        /// <returns>The currently authenticated user's name, empty or null string if the user is anonymous.</returns>
        public static string GetCurrentUserName(this UITestContext context)
        {
            GoTo<CurrentUserController>(context);
            var userNameContainer = context.Get(By.CssSelector("pre")).Text;
            return userNameContainer["UserName: ".Length..];
        }

        /// <summary>
        /// Enables the feature with the given ID directly, without anything
        /// else happening on the admin Features page. The target app needs to
        /// have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static void EnableFeatureDirectly(this UITestContext context, string featureId) =>
            GoTo<ShellFeaturesController>(
                context,
                nameof(ShellFeaturesController.EnableFeatureDirectly),
                (nameof(featureId), featureId));

        /// <summary>
        /// Disables the feature with the given ID directly, without anything else happening on the admin Features page.
        /// The target app needs to have <c>Lombiq.Tests.UI.Shortcuts</c> enabled.
        /// </summary>
        public static void DisableFeatureDirectly(this UITestContext context, string featureId) =>
            GoTo<ShellFeaturesController>(
                context,
                nameof(ShellFeaturesController.DisableFeatureDirectly),
                (nameof(featureId), featureId));

        /// <summary>
        /// Turns the <c>Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench</c> feature on, then off, and checks if the
        /// operations indeed worked. This can be used to test if anything breaks when a feature is enabled or disabled.
        /// </summary>
        public static void ExecuteAndAssertTestFeatureToggle(this UITestContext context)
        {
            context.EnableFeatureDirectly("Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench");
            context.GoToRelativeUrl(FeatureToggleTestBenchUrl);
            context.Scope.Driver.PageSource.ShouldContain("The Feature Toggle Test Bench worked.");
            context.DisableFeatureDirectly("Lombiq.Tests.UI.Shortcuts.FeatureToggleTestBench");
            context.GoToRelativeUrl(FeatureToggleTestBenchUrl);
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
        public static void PurgeMediaCacheDirectly(this UITestContext context, bool toggleTheFeature = false)
        {
            if (toggleTheFeature)
            {
                context.EnableFeatureDirectly("Lombiq.Tests.UI.Shortcuts.MediaCachePurge");
            }

            GoTo<MediaCachePurgeController>(context, nameof(MediaCachePurgeController.PurgeMediaCacheDirectly));

            if (toggleTheFeature)
            {
                context.DisableFeatureDirectly("Lombiq.Tests.UI.Shortcuts.MediaCachePurge");
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
        public static void ExecuteRecipeDirectly(this UITestContext context, string recipeName) =>
            GoTo<RecipeController>(
                context,
                nameof(RecipeController.Execute),
                (nameof(recipeName), recipeName));

        /// <summary>
        /// Navigates to a page whose action method throws <see cref="InvalidOperationException"/>. This causes ASP.NET
        /// Core to display an error page.
        /// </summary>
        public static void GoToErrorPageDirectly(this UITestContext context) => GoTo<ErrorController>(context);

        private static void GoTo<TController>(
            UITestContext context,
            string action = "Index",
            params (string Key, string Value)[] arguments)
            where TController : Controller
        {
            var query = arguments?.Any() == true
                ? "?" + string.Join("&", arguments.Select(argument => $"{argument.Key}={WebUtility.UrlEncode(argument.Value)}"))
                : string.Empty;
            context.GoToRelativeUrl($"{AreaUrl}{typeof(TController).ControllerName()}/{action}{query}");
        }

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
