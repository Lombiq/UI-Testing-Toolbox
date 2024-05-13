using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using OrchardCore.ContentFields.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class NavigationUITestContextExtensions
{
    public static Task GoToHomePageAsync(this UITestContext context, bool onlyIfNotAlreadyThere = true) =>
        context.GoToRelativeUrlAsync("/", onlyIfNotAlreadyThere);

    public static Task GoToRelativeUrlAsync(this UITestContext context, string relativeUrl, bool onlyIfNotAlreadyThere = true) =>
        context.GoToAbsoluteUrlAsync(context.GetAbsoluteUri(relativeUrl), onlyIfNotAlreadyThere);

    public static Task GoToAdminRelativeUrlAsync(
        this UITestContext context,
        string urlWithoutAdminPrefix = null,
        bool onlyIfNotAlreadyThere = true)
    {
        if (string.IsNullOrEmpty(urlWithoutAdminPrefix)) return context.GoToDashboardAsync();

        return context.GoToAbsoluteUrlAsync(context.GetAbsoluteAdminUri(urlWithoutAdminPrefix), onlyIfNotAlreadyThere);
    }

    public static async Task SignInDirectlyAndGoToAdminRelativeUrlAsync(
        this UITestContext context,
        string urlWithoutAdminPrefix = null,
        bool onlyIfNotAlreadyThere = true,
        string email = DefaultUser.UserName)
    {
        await context.SignInDirectlyAsync(email);

        await GoToAdminRelativeUrlAsync(context, urlWithoutAdminPrefix, onlyIfNotAlreadyThere);
    }

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
        new(context.Scope.BaseUri, relativeUrl.TrimStart('/'));

    public static Uri GetAbsoluteAdminUri(this UITestContext context, string adminRelativeUrl)
    {
        var combinedUriString = context.AdminUrlPrefix + adminRelativeUrl;

        return context.GetAbsoluteUri(combinedUriString);
    }

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

    // AtataContext is used from UITestContext in GoToPage() methods so they're future-proof in the case Atata won't be
    // fully static. Also, with async code it's also necessary to re-set AtataContext.Current now, see:
    // https://github.com/atata-framework/atata/issues/364.

    public static async Task<T> GoToPageAsync<T>(this UITestContext context, bool navigate = true)
        where T : PageObject<T>
    {
        var page = context.ExecuteLogged(
            nameof(GoToPageAsync),
            typeof(T).FullName,
            () => context.Scope.AtataContext.Go.To<T>(navigate: navigate));

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

    public static async Task<T> GoToAdminPageAsync<T>(this UITestContext context, string relativeUrl = null)
        where T : PageObject<T>
    {
        var uri = context.GetAbsoluteAdminUri(relativeUrl);

        var page = context.ExecuteLogged(
            $"{typeof(T).FullName} - {uri.LocalPath}",
            typeof(T).FullName,
            () => context.Scope.AtataContext.Go.To<T>(url: uri.ToString()));

        await context.TriggerAfterPageChangeEventAsync();

        context.RefreshCurrentAtataContext();

        return page;
    }

    public static Task<OrchardCoreSetupPage> GoToSetupPageAsync(this UITestContext context, bool navigate = true) =>
        context.GoToPageAsync<OrchardCoreSetupPage>(navigate);

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
        var setupPage = await context.GoToSetupPageAsync(parameters?.RunSetupOnCurrentPage == false);
        setupPage = await setupPage.SetupOrchardCoreAsync(context, parameters);

        return setupPage.PageUri.Value;
    }

    public static Task<OrchardCoreRegistrationPage> GoToRegistrationPageAsync(this UITestContext context) =>
        context.GoToPageAsync<OrchardCoreRegistrationPage>();

    public static Task<OrchardCoreDashboardPage> GoToDashboardAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreDashboardPage>();

    public static Task<OrchardCoreContentItemsPage> GoToContentItemsPageAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreContentItemsPage>("/Contents/ContentItems");

    public static Task<OrchardCoreFeaturesPage> GoToFeaturesPageAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreFeaturesPage>("/Features");

    /// <summary>
    /// Reloads <see cref="AtataContext.Current"/> from the <see cref="UITestContext"/>. This is necessary during Atata
    /// operations (like within a page class) when writing async code.
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
        context.SwitchTo(locator => locator.Window(context.Driver.WindowHandles[^1]), "last window");

    /// <summary>
    /// Switches control back to the oldest previous window/tab.
    /// </summary>
    public static void SwitchToFirstWindow(this UITestContext context) =>
        context.SwitchTo(locator => locator.Window(context.Driver.WindowHandles[0]), "first window");

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
                driver => driver.ExecuteScript("return document.readyState").Equals("complete")));

    public static Task SetTaxonomyFieldByIndexAsync(this UITestContext context, string taxonomyId, int index)
    {
        var baseSelector = ByHelper.Css($".tags[data-taxonomy-content-item-id='{taxonomyId}']");
        return SetFieldDropdownByIndexAsync(context, baseSelector, index);
    }

    public static Task SetTaxonomyFieldByTextAsync(this UITestContext context, string taxonomyId, string text)
    {
        var baseSelector = ByHelper.Css($".tags[data-taxonomy-content-item-id='{taxonomyId}']");
        return SetFieldDropdownByTextAsync(context, baseSelector, text);
    }

    public static async Task SetContentPickerByDisplayTextAsync(this UITestContext context, string part, string field, string text)
    {
        var searchUrl = context.Get(ByHelper.GetContentPickerSelector(part, field)).GetAttribute("data-search-url");
        var index = await context.FetchWithBrowserContextAsync(
            HttpMethod.Get,
            searchUrl,
            async response =>
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IList<VueMultiselectItemViewModel>>(json);
                return result.IndexOf(result.First(item => item.DisplayText == text));
            });

        await context.SetContentPickerByIndexAsync(part, field, index);
    }

    public static Task SetContentPickerByIndexAsync(this UITestContext context, string part, string field, int index)
    {
        var baseSelector = ByHelper.GetContentPickerSelector(part, field);
        return SetFieldDropdownByIndexAsync(context, baseSelector, index);
    }

    private static async Task SetFieldDropdownByIndexAsync(UITestContext context, By baseSelector, int index)
    {
        var byItem = baseSelector
            .Then(ByHelper.Css($".multiselect__element:nth-child({index + 1}) .multiselect__option"))
            .Visible();

        while (!context.Exists(byItem.Safely()))
        {
            await context.ClickReliablyOnAsync(baseSelector.Then(By.CssSelector(".multiselect__select")));
        }

        await context.ClickReliablyOnAsync(byItem);
    }

    private static async Task SetFieldDropdownByTextAsync(UITestContext context, By baseSelector, string text)
    {
        var byItem = baseSelector
            .Then(By.XPath($"//span[contains(@class,'multiselect__option')]//span[text() = '{text}']"))
            .Visible();

        while (!context.Exists(byItem.Safely()))
        {
            await context.ClickReliablyOnAsync(baseSelector.Then(By.CssSelector(".multiselect__select")));
        }

        await context.ClickReliablyOnAsync(byItem);
    }

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickReliablyAsync(IWebElement, UITestContext, int)"/> so the <paramref
    /// name="context"/> doesn't have to be passed twice.
    /// </summary>
    /// <param name="maxTries">The maximum number of clicks attempted altogether, if retries are needed.</param>
    public static Task ClickReliablyOnAsync(this UITestContext context, By by, int maxTries = 3) =>
        context.Get(by).ClickReliablyAsync(context, maxTries);

    /// <summary>
    /// Reliably clicks on the link identified by the given text with <see
    /// cref="NavigationWebElementExtensions.ClickReliablyAsync(IWebElement, UITestContext, int)"/>.
    /// </summary>
    /// <param name="maxTries">The maximum number of clicks attempted altogether, if retries are needed.</param>
    public static Task ClickReliablyOnByLinkTextAsync(this UITestContext context, string linkText, int maxTries = 3) =>
        context.Get(By.LinkText(linkText)).ClickReliablyAsync(context, maxTries);

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickReliablyUntilPageLeaveAsync(IWebElement, UITestContext, TimeSpan?,
    /// TimeSpan?)"/> so the <paramref name="context"/> doesn't have to be passed twice.
    /// </summary>
    public static Task ClickReliablyOnUntilPageLeaveAsync(
        this UITestContext context,
        By by,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.Get(by).ClickReliablyUntilPageLeaveAsync(context, timeout, interval);

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickReliablyUntilUrlChangeAsync(IWebElement, UITestContext, TimeSpan?,
    /// TimeSpan?)"/> so the <paramref name="context"/> doesn't have to be passed twice.
    /// </summary>
    public static Task ClickReliablyOnUntilUrlChangeAsync(
        this UITestContext context,
        By by,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.Get(by).ClickReliablyUntilUrlChangeAsync(context, timeout, interval);

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
    /// Clicks on the first matching element, switches control to the JS alert/prompt box that's expected to appear,
    /// enters <paramref name="inputText"/> as keystrokes if it's not <see langword="null"/>, accepts the alert/prompt
    /// box, and switches control back to main document or first frame.
    /// </summary>
    public static void ClickAndAcceptPrompt(this UITestContext context, By by, string inputText = null)
    {
        // Using FindElement() here because ClickReliablyOnAsync() would throw an "Unexpected Alert Open" exception.
        context.Driver.FindElement(by).Click();

        var alert = context.Driver.SwitchTo().Alert();
        if (inputText != null) alert.SendKeys(inputText);
        alert.Accept();
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

    public static Task GoToContentItemByIdAsync(this UITestContext context, string contentItemId) =>
        context.GoToRelativeUrlAsync("/Contents/ContentItems/" + contentItemId);

    /// <summary>
    /// A method to perform a drag and drop action from a source element to a destination element.
    /// </summary>
    /// <param name="sourceElementBy">The source element, that should be dragged and dropped.</param>
    /// <param name="destinationBy">The destination element, where the source element should be dropped.</param>
    public static void DragAndDrop(this UITestContext context, By sourceElementBy, By destinationBy) =>
        new Actions(context.Driver).DragAndDrop(context.Get(sourceElementBy), context.Get(destinationBy))
            .Build()
            .Perform();

    /// <summary>
    /// A method to perform a drag and drop action from a source element to an offset.
    /// </summary>
    /// <param name="sourceElementBy">The source element, that should be dragged and dropped.</param>
    /// <param name="offsetX">The x offset in pixels.</param>
    /// <param name="offsetY">The y offset in pixels.</param>
    public static void DragAndDropToOffset(this UITestContext context, By sourceElementBy, int offsetX, int offsetY) =>
        new Actions(context.Driver).DragAndDropToOffset(context.Get(sourceElementBy), offsetX, offsetY)
            .Build()
            .Perform();
}
