using Atata;
using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete. These are only used in obsolete extension methods.
using OrchardCoreContentItemsPage = Lombiq.Tests.UI.Pages.OrchardCoreContentItemsPage;
using OrchardCoreDashboardPage = Lombiq.Tests.UI.Pages.OrchardCoreDashboardPage;
using OrchardCoreFeaturesPage = Lombiq.Tests.UI.Pages.OrchardCoreFeaturesPage;
using OrchardCoreLoginPage = Lombiq.Tests.UI.Pages.OrchardCoreLoginPage;
using OrchardCoreRegistrationPage = Lombiq.Tests.UI.Pages.OrchardCoreRegistrationPage;
using OrchardCoreSetupPage = Lombiq.Tests.UI.Pages.OrchardCoreSetupPage;
using OrchardCoreSetupPageParameters = Lombiq.Tests.UI.Pages.OrchardCoreSetupParameters;
#pragma warning restore CS0618 // Type or member is obsolete. These are only used in obsolete extension methods.

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
        urlWithoutAdminPrefix ??= string.Empty;

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

                // Navigation can sometimes not happen on the first try.
                await context.DoWithRetriesUntilNavigationHasOccurredOrFailAsync(
                    () => context.Driver.Navigate().GoToUrlAsync(absoluteUri));

                await context.Configuration.Events.AfterNavigation
                    .InvokeAsync<NavigationEventHandler>(eventHandler => eventHandler(context, absoluteUri));
            });

    public static Uri GetCurrentUri(this UITestContext context) => new(context.Driver?.Url ?? context.TestStartUri.AbsoluteUri);

    public static string GetCurrentAbsolutePath(this UITestContext context) => context.GetCurrentUri().AbsolutePath;

    // A simple new(context.Scope.BaseUri, relativeUrl.TrimStart('/')) would work for most cases but not when using
    // tenants with a RequestUrlPrefix, because relativeUrl would then be relative to the host.
    public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
        new(context.Scope.BaseUri.OriginalString.TrimEnd('/') + "/" + relativeUrl.TrimStart('/'));

    public static Uri GetAbsoluteAdminUri(this UITestContext context, string adminRelativeUrl)
    {
        adminRelativeUrl ??= string.Empty;
        var combinedUriString = context.AdminUrlPrefix + adminRelativeUrl.Trim();

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
    [Obsolete("Methods using Page<> classes will be removed in the next version. Use " +
              $"{nameof(TypedRouteUITestContextExtensions.GoToAsync)} instead.")]
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

    [Obsolete("Methods using Page<> classes will be removed in the next version. Use " +
              $"{nameof(TypedRouteUITestContextExtensions.GoToAsync)} instead.")]
    public static async Task<T> GoToPageAsync<T>(this UITestContext context, string relativeUrl)
        where T : PageObject<T>
    {
        var page = await context.ExecuteLoggedAsync(
            $"{typeof(T).FullName} - {relativeUrl}",
            typeof(T).FullName,
            async () =>
            {
                T pageInternal = null;

                await context.DoWithRetriesUntilNavigationHasOccurredOrFailAsync(
                    () =>
                    {
                        pageInternal = context.Scope.AtataContext.Go.To<T>(
                            url: context.GetAbsoluteUri(relativeUrl).ToString());
                        return Task.CompletedTask;
                    });

                return pageInternal;
            });

        await context.TriggerAfterPageChangeEventAsync();

        context.RefreshCurrentAtataContext();

        return page;
    }

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToAdminRelativeUrlAsync)} instead.")]
    public static async Task<T> GoToAdminPageAsync<T>(this UITestContext context, string relativeUrl = null)
        where T : PageObject<T>
    {
        var uri = context.GetAbsoluteAdminUri(relativeUrl);

        var page = await context.ExecuteLoggedAsync(
            $"{typeof(T).FullName} - {uri.LocalPath}",
            typeof(T).FullName,
            async () =>
            {
                T pageInternal = null;

                await context.DoWithRetriesUntilNavigationHasOccurredOrFailAsync(
                    () =>
                    {
                        pageInternal = context.Scope.AtataContext.Go.To<T>(url: uri.ToString());
                        return Task.CompletedTask;
                    });

                return pageInternal;
            });

        await context.TriggerAfterPageChangeEventAsync();

        context.RefreshCurrentAtataContext();

        return page;
    }

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToRelativeUrlAsync)}(\"/\") instead.")]
    public static Task<OrchardCoreSetupPage> GoToSetupPageAsync(this UITestContext context, bool navigate = true) =>
        context.GoToPageAsync<OrchardCoreSetupPage>(navigate);

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToLoginAsync)} instead.")]
    public static Task<OrchardCoreLoginPage> GoToLoginPageAsync(this UITestContext context) =>
        context.GoToPageAsync<OrchardCoreLoginPage>();

    public static Task GoToLoginAsync(this UITestContext context) =>
        context.GoToRelativeUrlAsync("/Login");

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToSetupAndSetupOrchardCoreAsync)} instead.")]
    public static Task<Uri> GoToSetupPageAndSetupOrchardCoreAsync(this UITestContext context, string recipeId) =>
        context.GoToSetupPageAndSetupOrchardCoreAsync(
            new OrchardCoreSetupPageParameters(context)
            {
                RecipeId = recipeId,
            });

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToSetupAndSetupOrchardCoreAsync)} instead.")]
    public static async Task<Uri> GoToSetupPageAndSetupOrchardCoreAsync(
        this UITestContext context,
        OrchardCoreSetupPageParameters parameters = null)
    {
        var setupPage = await context.GoToSetupPageAsync(parameters?.RunSetupOnCurrentPage == false);
        setupPage = await setupPage.SetupOrchardCoreAsync(context, parameters);

        return setupPage.PageUri.Value;
    }

    public static Task<Uri> GoToSetupAndSetupOrchardCoreAsync(
        this UITestContext context,
        string recipeId,
        bool shouldBeSuccess = true) =>
        context.GoToSetupAndSetupOrchardCoreAsync(
            new OrchardCoreSetupParameters(context, recipeId),
            shouldBeSuccess);

    public static async Task<Uri> GoToSetupAndSetupOrchardCoreAsync(
        this UITestContext context,
        OrchardCoreSetupParameters parameters = null,
        bool shouldBeSuccess = true)
    {
        parameters ??= new(context);

        if (!parameters.RunSetupOnCurrentPage)
        {
            await context.GoToAbsoluteUrlAsync(parameters.SetupUri ?? context.TestStartUri);
        }

        await context.SetupOrchardCoreAsync(parameters);
        context.CheckExistence(OrchardCoreSetupParameters.FinishSetupSelector, !shouldBeSuccess);

        return new(context.Driver.Url);
    }

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToRegistrationAsync)} instead.")]
    public static Task<OrchardCoreRegistrationPage> GoToRegistrationPageAsync(this UITestContext context) =>
        context.GoToPageAsync<OrchardCoreRegistrationPage>();

    public static Task GoToRegistrationAsync(this UITestContext context) =>
        context.GoToRelativeUrlAsync('/' + UserRegistrationParameters.DefaultUrl);

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToDashboardAsync)} method instead.")]
    public static Task<OrchardCoreDashboardPage> GoToDashboardPageAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreDashboardPage>();

    public static Task GoToDashboardAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync(urlWithoutAdminPrefix: string.Empty, onlyIfNotAlreadyThere: false);

    [Obsolete("Methods using Page<> classes will be removed in the next version. Use " +
              $"{nameof(OrchardCoreDashboardUITestContextExtensions.GoToContentItemListAsync)} instead")]
    public static Task<OrchardCoreContentItemsPage> GoToContentItemsPageAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreContentItemsPage>("/Contents/ContentItems");

    [Obsolete($"Methods using Page<> classes will be removed in the next version. Use {nameof(GoToFeaturesAsync)} instead.")]
    public static Task<OrchardCoreFeaturesPage> GoToFeaturesPageAsync(this UITestContext context) =>
        context.GoToAdminPageAsync<OrchardCoreFeaturesPage>("/Features");

    /// <summary>
    /// Navigate to the Features admin configuration page.
    /// </summary>
    public static Task GoToFeaturesAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync("/Features");

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

    /// <summary>
    /// Waits until the HTML document is ready, i.e. the page has fully loaded.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the page has loaded within the allotted time frame, <see langword="false"/> otherwise.
    /// </returns>
    // Taken from: https://stackoverflow.com/a/36590395.
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
        var contentPickerBy = ByHelper.GetContentPickerSelector(part, field);

        await context.ClickAndFillInWithRetriesAsync(
            contentPickerBy.Then(By.ClassName("multiselect__input")).OfAnyVisibility(),
            text);

        await SetFieldDropdownByTextAsync(context, contentPickerBy, text);
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

    /// <inheritdoc cref="ClickReliablyOnUntilNavigationHasOccurredAsync(UITestContext, By, TimeSpan?, TimeSpan?)"/>
    [Obsolete("Use ClickReliablyOnUntilNavigationHasOccurredAsync instead.")]
    public static Task ClickReliablyOnUntilPageLeaveAsync(
        this UITestContext context,
        By by,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.ClickReliablyOnUntilNavigationHasOccurredAsync(by, timeout, interval);

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickReliablyUntilNavigationHasOccurredAsync"/> so the <paramref
    /// name="context"/> doesn't have to be passed twice.
    /// </summary>
    public static Task ClickReliablyOnUntilNavigationHasOccurredAsync(
        this UITestContext context,
        By by,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.Get(by).ClickReliablyUntilNavigationHasOccurredAsync(context, timeout, interval);

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickReliablyUntilUrlChangeAsync"/> so the <paramref name="context"/>
    /// doesn't have to be passed twice.
    /// </summary>
    public static Task ClickReliablyOnUntilUrlChangeAsync(
        this UITestContext context,
        By by,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.Get(by).ClickReliablyUntilUrlChangeAsync(context, timeout, interval);

    /// <summary>
    /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and <see
    /// cref="NavigationWebElementExtensions.ClickWithScriptAsync(IWebElement, UITestContext)"/> so the <paramref
    /// name="context"/> doesn't have to be passed twice.
    /// </summary>
    public static Task ClickOnWithScriptAsync(this UITestContext context, By by) =>
        context.Get(by).ClickWithScriptAsync(context);

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
    [Obsolete("Use RefreshAsync instead. That also runs NavigationEventHandlers, including checking the browser logs.")]
    public static void Refresh(this UITestContext context) => context.Scope.Driver.Navigate().Refresh();

    /// <summary>
    /// Refreshes (reloads) the current page.
    /// </summary>
    public static Task RefreshAsync(this UITestContext context) =>
        context.ExecuteLoggedAsync(
            nameof(RefreshAsync),
            async () =>
            {
                var absoluteUri = context.GetCurrentUri();

                await context.Configuration.Events.BeforeNavigation
                    .InvokeAsync<NavigationEventHandler>(eventHandler => eventHandler(context, absoluteUri));

                context.Scope.Driver.Navigate().Refresh();

                await context.Configuration.Events.AfterNavigation
                    .InvokeAsync<NavigationEventHandler>(eventHandler => eventHandler(context, absoluteUri));
            });

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

    /// <summary>
    /// A method to filter for an item on one of the admin pages.
    /// </summary>
    /// <param name="itemName">The element we should search or filter for. It could be for example a workflow too,
    /// that's why it's not called "contentItem".</param>
    public static async Task FilterOnAdminAsync(this UITestContext context, string itemName)
    {
        if (context.Exists(By.Id("Options_Search").Safely()))
        {
            await context.ClickAndFillInWithRetriesAsync(By.Id("Options_Search"), itemName);
        }
        else if (context.Exists(By.Id("Options_SearchText")))
        {
            await context.ClickAndFillInWithRetriesAsync(By.Id("Options_SearchText"), itemName);
        }

        // Normally we would trigger filtering by pressing the "Enter" key. The filter submit button is hidden, so we
        // have to use JS to click on it.
        context.ExecuteScript("document.getElementById('submitFilter').click();");
    }

    /// <summary>
    /// A method to filter for an item on the admin page with a search box that has search-box ID.
    /// </summary>
    /// <param name="text">Can be anything that's appropriate in the search input.</param>
    public static async Task FilterOnAdminWithSearchBoxAsync(this UITestContext context, string text)
    {
        await context.ClickAndFillInWithRetriesAsync(By.Id("search-box"), text);

        // Normally we would trigger filtering by pressing the "Enter" key. The filter submit button is hidden, so we
        // have to use JS to click on it.
        context.ExecuteScript("document.getElementById('submitFilter').click();");
    }

    /// <summary>
    /// Clicks on the <paramref name="byDropdownButton"/> until the Bootstrap dropdown menu appears (up to 3 tries) and
    /// then clicks on the menu item with the <paramref name="menuItemLinkText"/> within the dropdown menu's context.
    /// </summary>
    /// <param name="context">The current UI test context.</param>
    /// <param name="byDropdownButton">The path of the button that reveals the Bootstrap dropdown menu.</param>
    /// <param name="menuItemLinkText">The text of the dropdown menu item.</param>
    public static Task SelectFromBootstrapDropdownReliablyAsync(
        this UITestContext context,
        By byDropdownButton,
        string menuItemLinkText) =>
        SelectFromBootstrapDropdownReliablyAsync(context, context.Get(byDropdownButton), By.LinkText(menuItemLinkText));

    /// <summary>
    /// Clicks on the <paramref name="dropdownButton"/> until the Bootstrap dropdown menu appears with retries and then
    /// clicks on the <paramref name="byLocalMenuItem"/> within the dropdown menu's context.
    /// </summary>
    /// <param name="context">The current UI test context.</param>
    /// <param name="dropdownButton">The button that reveals the Bootstrap dropdown menu.</param>
    /// <param name="byLocalMenuItem">
    /// The path inside the dropdown menu. If <see langword="null"/> then no selection (clicking) will be made, and the
    /// dropdown is left open.
    /// </param>
    public static Task SelectFromBootstrapDropdownReliablyAsync(
        this UITestContext context,
        IWebElement dropdownButton,
        By byLocalMenuItem)
    {
        var byDropdownMenu = By.XPath("./following-sibling::*[contains(@class, 'dropdown-menu')]");

        return ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                await dropdownButton.ClickReliablyAsync(context);

                var dropdownMenu = dropdownButton.Get(byDropdownMenu);

                if (byLocalMenuItem == null) return true;

                await dropdownMenu.Get(byLocalMenuItem).ClickReliablyAsync(context);
                return true;
            },
            cancellationToken: context.Configuration.TestCancellationToken);
    }
}
