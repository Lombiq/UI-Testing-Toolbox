using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Globalization;
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
                $"{absoluteUri} ({(onlyIfNotAlreadyThere ? "navigating also" : "not navigating")} if already there)",
                () =>
                {
                    if (onlyIfNotAlreadyThere && context.GetCurrentUri() == absoluteUri) return;

                    context.Configuration.Events.BeforeNavigation?.Invoke(context, absoluteUri);
                    context.Driver.Navigate().GoToUrl(absoluteUri);
                    context.Configuration.Events.AfterNavigation?.Invoke(context, absoluteUri);
                });

        public static Uri GetCurrentUri(this UITestContext context) => new(context.Driver.Url);

        public static string GetCurrentAbsolutePath(this UITestContext context) => context.GetCurrentUri().AbsolutePath;

        public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
            new(context.Scope.BaseUri, relativeUrl);

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

        public static T GoToPage<T>(this UITestContext context)
            where T : PageObject<T> =>
            context.ExecuteLogged(
                nameof(GoToPage),
                typeof(T).FullName,
                () => context.Scope.AtataContext.Go.To<T>());

        public static T GoToPage<T>(this UITestContext context, string relativeUrl)
            where T : PageObject<T> =>
            context.ExecuteLogged(
                nameof(GoToPage),
                $"{typeof(T).FullName} - {relativeUrl}",
                () => context.Scope.AtataContext.Go.To<T>(url: context.GetAbsoluteUri(relativeUrl).ToString()));

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
        public static void SwitchToLastWindow(this UITestContext context) =>
            context.SwitchTo(locator => locator.Window(context.Driver.WindowHandles.Last()), "last window");

        /// <summary>
        /// Switches control back to the oldest previous window/tab.
        /// </summary>
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

        public static DateTime GetDatePicker(this UITestContext context, string id) =>
            DateTime.ParseExact(
                context.Get(By.Id(id)).GetAttribute("value"),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

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

        /// <summary>
        /// Switches control to JS alert box, accepts it, and switches control back to main document or first frame.
        /// </summary>
        public static void AcceptAlert(this UITestContext context)
        {
            context.Driver.SwitchTo().Alert().Accept();
            context.Driver.SwitchTo().DefaultContent();
        }

        /// <summary>
        /// Switches control to JS alert box, dismisses it, and switches control back to main document or first frame.
        /// </summary>
        public static void DismissAlert(this UITestContext context)
        {
            context.Driver.SwitchTo().Alert().Dismiss();
            context.Driver.SwitchTo().DefaultContent();
        }
    }
}
