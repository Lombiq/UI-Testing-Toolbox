using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Some useful shortcuts for test execution using the Lombiq.Tests.UI.Shortcuts module. Note that you have to have
    /// it enabled in the app for these to work.
    /// </summary>
    public static class ShortcutsUITestContextExtensions
    {
        /// <summary>
        /// Authenticates the client with the given user account. Note that this will execute a direct sign in without
        /// anything else happening on the login page. The target app needs to have Lombiq.Tests.UI.Shortcuts enabled.
        /// </summary>
        public static void SignInDirectly(this UITestContext context, string userName) =>
            context.GoToRelativeUrl("/Lombiq.Tests.UI.Shortcuts/Account/SignInDirectly?userName=" + userName);

        /// <summary>
        /// Retrieves the currently authenticated user's name, if any. The target app needs to have
        /// Lombiq.Tests.UI.Shortcuts enabled.
        /// </summary>
        /// <returns>The currently authenticated user's name, empty or null string if the user is anonymous.</returns>
        public static string GetCurrentUserName(this UITestContext context) =>
            context.GoToPage<CurrentUserPage>().LoggedInUser.Value;
    }
}
