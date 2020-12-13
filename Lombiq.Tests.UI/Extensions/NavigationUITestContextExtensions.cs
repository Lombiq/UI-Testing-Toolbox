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

        public static void GoToRelativeUrl(this UITestContext context, string relativeUrl, bool onlyIfNotAlreadyThere = true) =>
            context.GoToAbsoluteUrl(context.GetAbsoluteUri(relativeUrl), onlyIfNotAlreadyThere);

        public static void GoToAbsoluteUrl(this UITestContext context, Uri absoluteUri, bool onlyIfNotAlreadyThere = true) =>
            context.ExecuteLogged(
                nameof(GoToAbsoluteUrl),
                absoluteUri.ToString(),
                () =>
                {
                    if (onlyIfNotAlreadyThere && new Uri(context.Driver.Url) == absoluteUri) return;

                    context.Driver.Navigate().GoToUrl(absoluteUri);
                });

        public static void SignOutDirectlyThenSignInDirectlyAndGoToHomepage(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignOutDirectly();
            context.SignInDirectlyAndGoToHomepage(email);
        }

        public static void SignInDirectlyAndGoToHomepage(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignInDirectly(email);
            context.GoToHomePage();
        }

        public static void SignOutDirectlyThenSignInDirectlyAndGoToDashboard(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignOutDirectly();
            context.SignInDirectlyAndGoToDashboard(email);
        }

        public static void SignInDirectlyAndGoToDashboard(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignInDirectly(email);
            context.GoToDashboard();
        }

        public static void SignOutDirectlyThenSignInDirectly(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            context.SignOutDirectly();
            context.SignInDirectly(email);
        }

        public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
            new Uri(context.Scope.BaseUri, relativeUrl);

        public static T GoToPage<T>(this UITestContext context)
            where T : PageObject<T> =>
            context.ExecuteLogged(
                nameof(GoToPage),
                typeof(T).FullName,
                () =>
                {
                    context.Scope.SetContextAsCurrent();
                    return Go.To<T>();
                });

        public static T GoToPage<T>(this UITestContext context, string relativeUrl)
            where T : PageObject<T> =>
            context.ExecuteLogged(
                nameof(GoToPage),
                $"{typeof(T).FullName} - {relativeUrl}",
                () =>
                {
                    var uri = context.GetAbsoluteUri(relativeUrl);

                    context.Scope.SetContextAsCurrent();
                    return Go.To<T>(url: uri.ToString());
                });

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

            context.GoToAbsoluteUrl(context.SmtpServiceRunningContext.WebUIUri);
        }

        public static void SwitchTo(this UITestContext context, Action<ITargetLocator> switchOperation, string targetDescription) =>
            context.ExecuteLogged(
                nameof(SwitchTo),
                targetDescription,
                () => switchOperation(context.Driver.SwitchTo()));

        /// <summary>
        /// Switches control back to the most recent previous window/tab.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call <see cref="UITestContext.AssertBrowserLogAsync"/> before last leaving a window/tab if you want the
        /// browser log to be checked. Otherwise only the last active window's logs will be checked.
        /// </para>
        /// </remarks>
        public static void SwitchToLastWindow(this UITestContext context) =>
            context.SwitchTo(locator => locator.Window(context.Driver.WindowHandles.Last()), "last window");

        /// <summary>
        /// Switches control back to the oldest previous window/tab.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call <see cref="UITestContext.AssertBrowserLogAsync"/> before last leaving a window/tab if you want the
        /// browser log to be checked. Otherwise only the last active window's logs will be checked.
        /// </para>
        /// </remarks>
        public static void SwitchToFirstWindow(this UITestContext context) =>
            context.SwitchTo(locator => locator.Window(context.Driver.WindowHandles.First()), "first window");

        public static void SwitchToFrame0(this UITestContext context) =>
            context.SwitchTo(locator => locator.Frame(0), "frame 0");

        // Taken from: https://stackoverflow.com/a/36590395
        public static bool WaitForPageLoad(this UITestContext context) =>
            context.ExecuteLogged(
                nameof(WaitForPageLoad),
                context.Driver.Url,
                () => new WebDriverWait(context.Driver, TimeSpan.FromSeconds(10)).Until(
                    d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete")));

        public static void SetDropdown<T>(this UITestContext context, string selectId, T value)
            where T : Enum
        {
            context.ClickReliablyOn(By.Id(selectId));
            context.Get(By.CssSelector($"#{selectId} option[value='{(int)(object)value}']")).Click();
        }

        public static void SetDropdownByText(this UITestContext context, string selectId, string value)
        {
            context.ClickReliablyOn(By.Id(selectId));
            context.Get(By.XPath($"//select[@id='{selectId}']//option[contains(., '{value}')]")).Click();
        }

        public static void SetDatePicker(this UITestContext context, string id, DateTime value) =>
            context.ExecuteScript($"document.getElementById('{id}').value = '{value:yyyy-MM-dd}';");

        /// <summary>
        /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
        /// cref="NavigationWebElementExtensions.ClickReliably(IWebElement,UITestContext)"/> so the <paramref
        /// name="context"/> doesn't have to be passed twice.
        /// </summary>
        public static void ClickReliablyOn(this UITestContext context, By by) => context.Get(by).ClickReliably(context);

        /// <summary>
        /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
        /// cref="NavigationWebElementExtensions.ClickReliablyUntilPageLeave(IWebElement, UITestContext, TimeSpan?,
        /// TimeSpan?)"/> so the <paramref name="context"/> doesn't have to be passed twice.
        /// </summary>
        public static void ClickReliablyOnUntilPageLeave(
            this UITestContext context,
            By by,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.Get(by).ClickReliablyUntilPageLeave(context, timeout, interval);
    }
}
