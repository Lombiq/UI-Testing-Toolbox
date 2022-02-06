using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.MonkeyTesting.UrlFilters;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Provides a set of extension methods for monkey testing.
    /// </summary>
    public static class MonkeyTestingUITestContextExtensions
    {
        /// <inheritdoc cref="TestFrontendAnonymouslyAsMonkeyRecursivelyAsync(UITestContext, MonkeyTestingOptions, string)"/>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestFrontendAnonymouslyAsMonkeyRecursively(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string startingRelativeUrl = "/")
        {
            TestFrontendAnonymouslyAsMonkeyRecursivelyAsync(context, options, startingRelativeUrl).GetAwaiter().GetResult();
            return context;
        }

        /// <summary>
        /// Tests the frontend (i.e. pages NOT under /admin) as monkey as an anonymous user.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="startingRelativeUrl">
        /// The relative URL to start monkey testing from. Defaults to <c>"/"</c>.
        /// </param>
        public static Task TestFrontendAnonymouslyAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string startingRelativeUrl = "/") =>
            TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(context, options, signInDirectlyWithUserName: null, startingRelativeUrl);

        /// <inheritdoc cref="TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(UITestContext, MonkeyTestingOptions, string, string)"/>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestFrontendAuthenticatedAsMonkeyRecursively(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/")
        {
            TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(context, options, signInDirectlyWithUserName, startingRelativeUrl)
                .GetAwaiter()
                .GetResult();
            return context;
        }

        /// <summary>
        /// Tests the frontend (i.e. pages NOT under /admin) as monkey as an authenticated user.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="signInDirectlyWithUserName">
        /// The username to sign in with directly. Defaults to <see cref="DefaultUser.UserName"/>.
        /// </param>
        /// <param name="startingRelativeUrl">
        /// The relative URL to start monkey testing from. Defaults to <c>"/"</c>.
        /// </param>
        public static Task TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/")
        {
            if (!string.IsNullOrEmpty(signInDirectlyWithUserName)) context.SignInDirectly(signInDirectlyWithUserName);
            if (!string.IsNullOrEmpty(startingRelativeUrl)) context.GoToRelativeUrl(startingRelativeUrl);

            options ??= new MonkeyTestingOptions();
            options.UrlFilters.Add(new NotAdminMonkeyTestingUrlFilter());
            return context.TestCurrentPageAsMonkeyRecursivelyAsync(options);
        }

        /// <inheritdoc cref="TestAdminAsMonkeyRecursivelyAsync(UITestContext, MonkeyTestingOptions, string, string)"/>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestAdminAsMonkeyRecursively(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/admin")
        {
            TestAdminAsMonkeyRecursivelyAsync(context, options, signInDirectlyWithUserName, startingRelativeUrl).GetAwaiter().GetResult();
            return context;
        }

        /// <summary>
        /// Tests the admin area (i.e. pages under /admin) as monkey.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="signInDirectlyWithUserName">
        /// The username to sign in with directly. Defaults to <see cref="DefaultUser.UserName"/>.
        /// </param>
        /// <param name="startingRelativeUrl">
        /// The relative URL to start monkey testing from. Defaults to <c>"/admin"</c>.
        /// </param>
        public static Task TestAdminAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/admin")
        {
            if (!string.IsNullOrEmpty(signInDirectlyWithUserName)) context.SignInDirectly(signInDirectlyWithUserName);
            if (!string.IsNullOrEmpty(startingRelativeUrl)) context.GoToRelativeUrl(startingRelativeUrl);

            options ??= new MonkeyTestingOptions();
            options.UrlFilters.Add(new AdminMonkeyTestingUrlFilter());
            return context.TestCurrentPageAsMonkeyRecursivelyAsync(options);
        }

        /// <inheritdoc cref="TestCurrentPageAsMonkeyAsync(UITestContext, MonkeyTestingOptions, int?)"/>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestCurrentPageAsMonkey(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            int? randomSeed = null)
        {
            TestCurrentPageAsMonkeyAsync(context, options, randomSeed).GetAwaiter().GetResult();
            return context;
        }

        /// <summary>
        /// Tests the current page as monkey. Test finishes by timeout or when the current page is left during testing.
        /// Can optionally take <paramref name="randomSeed"/> value to reproduce a test with the same randomization.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="randomSeed">The random seed that is used by the Gremlins.js script.</param>
        public static Task TestCurrentPageAsMonkeyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            int? randomSeed = null) =>
            new MonkeyTester(context, options).TestOnePageAsync(randomSeed);

        /// <inheritdoc cref="TestCurrentPageAsMonkeyRecursivelyAsync(UITestContext, MonkeyTestingOptions)"/>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestCurrentPageAsMonkeyRecursively(
            this UITestContext context,
            MonkeyTestingOptions options = null)
        {
            TestCurrentPageAsMonkeyRecursivelyAsync(context, options).GetAwaiter().GetResult();
            return context;
        }

        /// <summary>
        /// Tests the current page as monkey recursively. When the current page is left during test, continues to test
        /// the other page and so on. Each page is tested for <see cref="MonkeyTestingOptions.PageTestTime"/> value of
        /// <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        public static Task TestCurrentPageAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null) =>
            new MonkeyTester(context, options).TestRecursivelyAsync();
    }
}
