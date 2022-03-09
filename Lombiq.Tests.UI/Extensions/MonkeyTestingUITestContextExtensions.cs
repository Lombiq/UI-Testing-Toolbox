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
        public static async Task TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/")
        {
            if (!string.IsNullOrEmpty(signInDirectlyWithUserName)) await context.SignInDirectlyAsync(signInDirectlyWithUserName);
            if (!string.IsNullOrEmpty(startingRelativeUrl)) await context.GoToRelativeUrlAsync(startingRelativeUrl);

            options ??= new MonkeyTestingOptions();
            options.UrlFilters.Add(new NotAdminMonkeyTestingUrlFilter());
            await context.TestCurrentPageAsMonkeyRecursivelyAsync(options);
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
        public static async Task TestAdminAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/admin")
        {
            if (!string.IsNullOrEmpty(signInDirectlyWithUserName)) await context.SignInDirectlyAsync(signInDirectlyWithUserName);
            if (!string.IsNullOrEmpty(startingRelativeUrl)) await context.GoToRelativeUrlAsync(startingRelativeUrl);

            options ??= new MonkeyTestingOptions();
            options.UrlFilters.Add(new AdminMonkeyTestingUrlFilter());
            await context.TestCurrentPageAsMonkeyRecursivelyAsync(options);
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

        /// <summary>
        /// Tests the frontend (i.e. pages NOT under /admin) as monkey as an authenticated user and as an anonymous
        /// user.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="signInDirectlyWithUserName">
        /// The username to sign in with directly. Defaults to <see cref="DefaultUser.UserName"/>.
        /// </param>
        /// <param name="startingRelativeUrl">
        /// The relative URL to start monkey testing from. Defaults to <c>"/"</c>.
        /// </param>
        public static async Task TestFrontendAuthenticatedAndAnonymouslyAsMonkeyRecursivelyAsync(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            string signInDirectlyWithUserName = DefaultUser.UserName,
            string startingRelativeUrl = "/")
        {
            await TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(
                context,
                options,
                signInDirectlyWithUserName,
                startingRelativeUrl);
            await TestFrontendAnonymouslyAsMonkeyRecursivelyAsync(context, options, startingRelativeUrl);
        }
    }
}
