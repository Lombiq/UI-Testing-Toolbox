using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions
{
    public static class NavigationUITestContextExtensions
    {
        // The context is passed in to every method so they're future-proof in the case Atata won't be fully static.
        // Also, with async code it's also necessary to re-set AtataContext.Current now, see:
        // https://github.com/atata-framework/atata/issues/364

        public static void GoToHomePage(this UITestContext context) => context.GoToRelativeUrl("/");

        public static void GoToRelativeUrl(this UITestContext context, string relativeUrl, bool onlyIfNotAlreadyThere = true)
        {
            var uri = context.GetAbsoluteUri(relativeUrl);

            if (onlyIfNotAlreadyThere && new Uri(context.Driver.Url) == uri) return;

            context.Driver.Navigate().GoToUrl(uri);
        }

        public static void SignInDirectlyAndGoToHomepage(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignInDirectly(email);
            context.GoToHomePage();
        }

        public static void SignInDirectlyAndGoToDashboard(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignInDirectly(email);
            context.GoToDashboard();
        }

        public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
            new Uri(context.Scope.BaseUri, relativeUrl);

        public static T GoToPage<T>(this UITestContext context)
            where T : PageObject<T>
        {
            AtataContext.Current = context.Scope.AtataContext;
            return Go.To<T>();
        }

        public static T GoToPage<T>(this UITestContext context, string relativeUrl)
            where T : PageObject<T>
        {
            var uri = context.GetAbsoluteUri(relativeUrl);

            AtataContext.Current = context.Scope.AtataContext;
            return Go.To<T>(url: uri.ToString());
        }

        public static OrchardCoreDashboardPage GoToDashboard(this UITestContext context) =>
            context.GoToPage<OrchardCoreDashboardPage>();

        public static OrchardCoreSetupPage GoToSetupPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreSetupPage>();

        public static void GoToSmtpWebUI(this UITestContext context)
        {
            if (context.SmtpServiceRunningContext == null)
            {
                throw new InvalidOperationException(
                    "The SMTP service is not running. Did you turn it on with " +
                    nameof(OrchardCoreUITestExecutorConfiguration) + "." + nameof(OrchardCoreUITestExecutorConfiguration.UseSmtpService) +
                    " and could it properly start?");
            }

            context.Driver.Navigate().GoToUrl(context.SmtpServiceRunningContext.WebUIUri);
        }

        public static ITargetLocator SwitchTo(this UITestContext context) => context.Driver.SwitchTo();

        /// <summary>
        /// Switches control back to the most recent previous window/tab.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call <see cref="UITestContext.AssertBrowserLogAsync"/> before last leaving a window/tab if you want the
        /// browser log to be checked. Otherwise only the last active window's logs will be checked.
        /// </para>
        /// </remarks>
        public static IWebDriver SwitchToLastWindow(this UITestContext context) =>
            context.SwitchTo().Window(context.Driver.WindowHandles.Last());

        public static IWebDriver SwitchToFrame0(this UITestContext context) => context.SwitchTo().Frame(0);

        // Taken from: https://stackoverflow.com/a/36590395
        public static bool WaitForPageLoad(this UITestContext context) =>
            new WebDriverWait(context.Driver, TimeSpan.FromSeconds(10)).Until(
                d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
    }
}
