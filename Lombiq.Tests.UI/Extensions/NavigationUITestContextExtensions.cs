using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class NavigationUITestContextExtensions
    {
        public static Task GoToHomePageAsync(this UITestContext context) => context.GoToRelativeUrlAsync("/");

        public static Task GoToRelativeUrlAsync(this UITestContext context, string relativeUrl, bool onlyIfNotAlreadyThere = true) =>
            context.GoToAbsoluteUrlAsync(context.GetAbsoluteUri(relativeUrl), onlyIfNotAlreadyThere);

        public static Task GoToAbsoluteUrlAsync(this UITestContext context, Uri absoluteUri, bool onlyIfNotAlreadyThere = true) =>
            context.ExecuteLoggedAsync(
                nameof(GoToAbsoluteUrlAsync),
                $"{absoluteUri} ({(onlyIfNotAlreadyThere ? "navigating also" : "not navigating")} if already there)",
                async () =>
                {
                    if (onlyIfNotAlreadyThere && context.GetCurrentUri() == absoluteUri) return;

                    await context.Configuration.Events.BeforeNavigation
                        .InvokeAsync<NavigationEventHandler>(eventHandler => eventHandler(context, absoluteUri));

                    context.Driver.Navigate().GoToUrl(absoluteUri);

                    await context.Configuration.Events.AfterNavigation
                        .InvokeAsync<NavigationEventHandler>(eventHandler => eventHandler(context, absoluteUri));
                });

        public static Uri GetCurrentUri(this UITestContext context) => new(context.Driver.Url);

        public static string GetCurrentAbsolutePath(this UITestContext context) => context.GetCurrentUri().AbsolutePath;

        public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
            new(context.Scope.BaseUri, relativeUrl);

        public static async Task SignOutDirectlyThenSignInDirectlyAndGoToHomepageAsync(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            await context.SignOutDirectlyAsync();
            await context.SignInDirectlyAndGoToHomepageAsync(email);
        }

        public static async Task SignInDirectlyAndGoToHomepageAsync(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            await context.SignInDirectlyAsync(email);
            await context.GoToHomePageAsync();
        }

        public static async Task SignOutDirectlyThenSignInDirectlyAndGoToDashboardAsync(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            await context.SignOutDirectlyAsync();
            await context.SignInDirectlyAndGoToDashboardAsync(email);
        }

        public static async Task SignInDirectlyAndGoToDashboardAsync(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            await context.SignInDirectlyAsync(email);
            await context.GoToDashboardAsync();
        }

        public static async Task SignOutDirectlyThenSignInDirectlyAsync(
            this UITestContext context,
            string email = DefaultUser.UserName)
        {
            await context.SignOutDirectlyAsync();
            await context.SignInDirectlyAsync(email);
        }

        // AtataContext is used from UITestContext in GoToPage() methods so they're future-proof in the case Atata won't
        // be fully static. Also, with async code it's also necessary to re-set AtataContext.Current now, see:
        // https://github.com/atata-framework/atata/issues/364.

        public static async Task<T> GoToPageAsync<T>(this UITestContext context)
            where T : PageObject<T>
        {
            var page = context.ExecuteLogged(
                nameof(GoToPageAsync),
                typeof(T).FullName,
                () => context.Scope.AtataContext.Go.To<T>());

            await context.TriggerAfterPageChangeEventAsync();

            context.RefreshCurrentAtataContext();

            return page;
        }

        public static async Task<T> GoToPageAsync<T>(this UITestContext context, string relativeUrl)
            where T : PageObject<T>
        {
            var page = context.ExecuteLogged(
                $"{typeof(T).FullName} - {relativeUrl}",
                typeof(T).FullName,
                () => context.Scope.AtataContext.Go.To<T>(url: context.GetAbsoluteUri(relativeUrl).ToString()));

            await context.TriggerAfterPageChangeEventAsync();

            context.RefreshCurrentAtataContext();

            return page;
        }

        public static Task<OrchardCoreSetupPage> GoToSetupPageAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreSetupPage>();

        public static Task<OrchardCoreLoginPage> GoToLoginPageAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreLoginPage>();

        public static Task<Uri> GoToSetupPageAndSetupOrchardCoreAsync(this UITestContext context, string recipeId) =>
            context.GoToSetupPageAndSetupOrchardCoreAsync(
                new OrchardCoreSetupParameters(context)
                {
                    RecipeId = recipeId,
                });

        public static async Task<Uri> GoToSetupPageAndSetupOrchardCoreAsync(
            this UITestContext context,
            OrchardCoreSetupParameters parameters = null)
        {
            var setupPage = await context.GoToSetupPageAsync();
            setupPage = await setupPage.SetupOrchardCoreAsync(context, parameters);

            return setupPage.PageUri.Value;
        }

        public static Task<OrchardCoreRegistrationPage> GoToRegistrationPageAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreRegistrationPage>();

        public static Task<OrchardCoreDashboardPage> GoToDashboardAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreDashboardPage>();

        public static Task<OrchardCoreContentItemsPage> GoToContentItemsPageAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreContentItemsPage>();

        public static Task<OrchardCoreFeaturesPage> GoToFeaturesPageAsync(this UITestContext context) =>
            context.GoToPageAsync<OrchardCoreFeaturesPage>();

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

        public static Task SetTaxonomyFieldByIndexAsync(this UITestContext context, string taxonomyId, int index)
        {
            var baseSelector = FormattableString.Invariant($".tags[data-taxonomy-content-item-id='{taxonomyId}']");
            return SetFieldDropdownByIndexAsync(context, baseSelector, index);
        }

        public static Task SetContentPickerByIndexAsync(this UITestContext context, string part, string field, int index)
        {
            var baseSelector = FormattableString.Invariant($"*[data-part='{part}'][data-field='{field}']");
            return SetFieldDropdownByIndexAsync(context, baseSelector, index);
        }

        private static async Task SetFieldDropdownByIndexAsync(UITestContext context, string baseSelector, int index)
        {
            var byItem = By.CssSelector(FormattableString.Invariant(
                $"{baseSelector} .multiselect__element:nth-child({index + 1}) .multiselect__option"));

            while (!context.Exists(byItem.Safely()))
            {
                await context.ClickReliablyOnAsync(By.CssSelector(baseSelector + " .multiselect__select"));
            }

            await context.ClickReliablyOnAsync(byItem);
        }

        /// <summary>
        /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
        /// cref="NavigationWebElementExtensions.ClickReliablyAsync(IWebElement,UITestContext)"/> so the <paramref
        /// name="context"/> doesn't have to be passed twice.
        /// </summary>
        public static Task ClickReliablyOnAsync(this UITestContext context, By by) => context.Get(by).ClickReliablyAsync(context);

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
