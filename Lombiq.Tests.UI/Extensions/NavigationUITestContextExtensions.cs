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

                    context.Configuration.Events.BeforeNavigation?.Invoke(context, absoluteUri).GetAwaiter().GetResult();
                    context.Driver.Navigate().GoToUrl(absoluteUri);
                    context.Configuration.Events.AfterNavigation?.Invoke(context, absoluteUri).GetAwaiter().GetResult();
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

        // AtataContext is used from UITestContext in GoToPage() methods so they're future-proof in the case Atata won't
        // be fully static. Also, with async code it's also necessary to re-set AtataContext.Current now, see:
        // https://github.com/atata-framework/atata/issues/364
        // The GoToPage() methods SHOULD NOT BE ASYNC, otherwise during subsequent operations AtataContext.Current will
        // be lost.

        public static T GoToPage<T>(this UITestContext context)
            where T : PageObject<T>
        {
            var page = context.ExecuteLogged(
                nameof(GoToPage),
                typeof(T).FullName,
                () => context.Scope.AtataContext.Go.To<T>());
            context.TriggerAfterPageChangeEventAsync().Wait();
            return page;
        }

        public static T GoToPage<T>(this UITestContext context, string relativeUrl)
            where T : PageObject<T>
        {
            var page = context.ExecuteLogged(
                $"{typeof(T).FullName} - {relativeUrl}",
                typeof(T).FullName,
                () => context.Scope.AtataContext.Go.To<T>(url: context.GetAbsoluteUri(relativeUrl).ToString()));
            context.TriggerAfterPageChangeEventAsync().Wait();
            return page;
        }

        public static OrchardCoreSetupPage GoToSetupPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreSetupPage>();

        public static OrchardCoreLoginPage GoToLoginPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreLoginPage>();

        public static OrchardCoreRegistrationPage GoToRegistrationPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreRegistrationPage>();

        public static OrchardCoreDashboardPage GoToDashboard(this UITestContext context) =>
            context.GoToPage<OrchardCoreDashboardPage>();

        public static OrchardCoreContentItemsPage GoToContentItemsPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreContentItemsPage>();

        public static OrchardCoreFeaturesPage GoToFeaturesPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreFeaturesPage>();

        /// <summary>
        /// Reloads <see cref="AtataContext.Current"/> from the <see cref="UITestContext"/>. This is necessary during
        /// Atata operations (like within a page class) when writing async code.
        /// </summary>
        public static void RefreshCurrentAtataContext(this UITestContext context) =>
            AtataContext.Current = context.Scope.AtataContext;

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

        /// <summary>
        /// Switches control back to the currently executing window/tab.
        /// </summary>
        public static void SwitchToCurrentWindow(this UITestContext context) =>
            context.SwitchTo(locator => locator.Window(context.Driver.CurrentWindowHandle), "current window");

        public static void SwitchToFrame0(this UITestContext context) =>
            context.SwitchTo(locator => locator.Frame(0), "frame 0");

        // Taken from: https://stackoverflow.com/a/36590395
        public static bool WaitForPageLoad(this UITestContext context) =>
            context.ExecuteLogged(
                nameof(WaitForPageLoad),
                context.Driver.Url,
                () => new WebDriverWait(context.Driver, TimeSpan.FromSeconds(10)).Until(
                    d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete")));

        public static void SetTaxonomyFieldByIndex(this UITestContext context, string taxonomyId, int index)
        {
            var baseSelector = FormattableString.Invariant($".tags[data-taxonomy-content-item-id='{taxonomyId}']");
            SetFieldDropdownByIndex(context, baseSelector, index);
        }

        public static void SetContentPickerByIndex(this UITestContext context, string part, string field, int index)
        {
            var baseSelector = FormattableString.Invariant($"*[data-part='{part}'][data-field='{field}']");
            SetFieldDropdownByIndex(context, baseSelector, index);
        }

        private static void SetFieldDropdownByIndex(UITestContext context, string baseSelector, int index)
        {
            var byItem = By.CssSelector(FormattableString.Invariant(
                $"{baseSelector} .multiselect__element:nth-child({index + 1}) .multiselect__option"));

            while (!context.Exists(byItem.Safely()))
            {
                context.ClickReliablyOn(By.CssSelector(baseSelector + " .multiselect__select"));
            }

            context.ClickReliablyOn(byItem);
        }

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

        /// <summary>
        /// Refreshes (reloads) the current page.
        /// </summary>
        public static void Refresh(this UITestContext context) => context.Scope.Driver.Navigate().Refresh();

        /// <summary>
        /// Checks whether the current page is the Orchard setup page.
        /// </summary>
        public static bool IsSetupPage(this UITestContext context) =>
            context.Driver.Title == "Setup" &&
            context.Driver.PageSource.Contains(
                @"<link type=""image/x-icon"" rel=""shortcut icon"" href=""/OrchardCore.Setup/favicon.ico"">");
    }
}
