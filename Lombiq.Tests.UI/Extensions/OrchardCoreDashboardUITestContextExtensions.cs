using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class OrchardCoreDashboardUITestContextExtensions
{
    public static Task FillContentItemTitleAsync(this UITestContext context, string title) =>
        context.ClickAndFillInWithRetriesAsync(By.Id("TitlePart_Title"), title);

    public static Task GoToEditorTabAsync(this UITestContext context, string tabText) =>
        context.ClickReliablyOnAsync(By.XPath($"//*[text()='{tabText}' and @class='nav-item nav-link']"));

    public static async Task ClickPublishAsync(this UITestContext context, bool withJavaScript = false)
    {
        if (withJavaScript)
        {
            context.ExecuteScript("document.querySelector('.publish-button, .publish.btn').click();");
        }
        else
        {
            await context.ClickReliablyOnAsync(By.Name("submit.Publish"));
        }
    }

    /// <summary>
    /// Clicks on the "Ok" button on the Bootstrap modal window.
    /// </summary>
    public static Task ClickModalOkAsync(this UITestContext context) => context.ClickReliablyOnAsync(By.Id("modalOkButton"));

    /// <inheritdoc cref="ClickPublishUntilNavigationHasOccurredAsync(UITestContext, bool, TimeSpan?, TimeSpan?)"/>/>
    [Obsolete("Use ClickPublishUntilNavigationHasOccurredAsync instead.")]
    public static Task ClickPublishUntilNavigationAsync(
        this UITestContext context,
        bool withJavaScript = false,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.ClickPublishUntilNavigationHasOccurredAsync(withJavaScript, timeout, interval);

    /// <summary>
    /// Sometimes the Publish button doesn't get clicked. This method retries pressing it up to 4 times with a 30 second
    /// interval between attempts. This should grant enough time to execute the publish action if the button actually
    /// got pressed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="timeout"/> and <paramref name="interval"/> have different default values from other similar
    /// methods that get theirs from the test configuration. These defaults are set to minimize the chance of an
    /// unintended early timeout or bounce effect because the publishing may take a longer time.
    /// </para>
    /// </remarks>
    public static Task ClickPublishUntilNavigationHasOccurredAsync(
        this UITestContext context,
        bool withJavaScript = false,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.DoWithRetriesUntilNavigationHasOccurredOrFailAsync(
            () => ClickPublishAsync(context, withJavaScript),
            timeout ?? TimeSpan.FromMinutes(2),
            interval ?? TimeSpan.FromSeconds(30));

    public static Task GoToContentItemListAsync(this UITestContext context, string filterContentType = null)
    {
        var query = string.IsNullOrEmpty(filterContentType)
            ? string.Empty
            : ("?q=type%3A" + filterContentType);
        return context.GoToAdminRelativeUrlAsync($"/Contents/ContentItems{query}");
    }

    public static async Task GoToContentItemListAndCreateNewAsync(this UITestContext context, string contentTypeText)
    {
        await context.GoToContentItemListAsync();
        await context.ClickNewContentItemAsync(contentTypeText);
    }

    /// <summary>
    /// Navigates to the page for creating a new content item of <paramref name="contentType"/>.
    /// </summary>
    public static Task CreateNewContentItemAsync(
        this UITestContext context,
        string contentType,
        bool onlyIfNotAlreadyThere = true) =>
            context.GoToAdminRelativeUrlAsync($"/Contents/ContentTypes/{contentType}/Create", onlyIfNotAlreadyThere);

    /// <summary>
    /// Navigates to the page for creating a new Page content item with the provided <paramref name="title"/>.
    /// </summary>
    public static async Task CreateNewPageContentItemAsync(
        this UITestContext context,
        string title,
        bool publish = true,
        bool checkSuccess = true)
    {
        await context.CreateNewContentItemAsync("Page");
        await context.ClickAndFillInWithRetriesAsync(By.Name("TitlePart.Title"), title);

        if (publish)
        {
            await context.ClickPublishAsync();
            if (checkSuccess)
            {
                context.ShouldBeSuccess();
            }
        }
    }

    /// <summary>
    /// Navigates to the Content Types page of the Orchard dashboard.
    /// </summary>
    public static Task GoToContentTypesListAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync("/ContentTypes/List");

    /// <summary>
    /// Navigates to the editor page of a content type on the Orchard dashboard.
    /// </summary>
    /// <param name="contentType">The technical name of the content type to open the editor of.</param>
    public static Task GoToContentTypeEditorAsync(this UITestContext context, string contentType) =>
        context.GoToAdminRelativeUrlAsync($"/ContentTypes/Edit/{contentType}");

    public static async Task ClickNewContentItemAsync(this UITestContext context, string contentItemName, bool dropdown = true)
    {
        if (dropdown)
        {
            await context.ClickReliablyOnAsync(By.Id("new-dropdown"));
            await context.ClickReliablyOnByLinkTextAsync(contentItemName);
        }
        else
        {
            await context.ClickReliablyOnByLinkTextAsync($"New {contentItemName}");
        }
    }

    public static Task GoToUsersAsync(this UITestContext context, string query = null) =>
        context.GoToAdminRelativeUrlAsync(
            string.IsNullOrWhiteSpace(query) ? "/Users/Index" : $"/Users/Index?q={WebUtility.UrlEncode(query)}");

    public static Task GoToContentItemEditorByIdAsync(this UITestContext context, string contentItemId) =>
        context.GoToAdminRelativeUrlAsync($"/Contents/ContentItems/{contentItemId}/Edit");

    public static Task GoToContentItemDisplayByIdAsync(this UITestContext context, string contentItemId) =>
        context.GoToAdminRelativeUrlAsync($"/Contents/ContentItems/{contentItemId}/Display");

    /// <summary>
    /// Navigate to the Features admin configuration page.
    /// </summary>
    public static Task GoToFeaturesAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync("Features");

    /// <summary>
    /// Navigate to the Features admin configuration page and search for the provided text.
    /// </summary>
    /// <returns>A collection of visible checkbox elements.</returns>
    public static async Task<IEnumerable<IWebElement>> GoToFeaturesAsync(this UITestContext context, string search)
    {
        await context.GoToFeaturesAsync();
        await context.ClickAndFillInWithRetriesAsync(By.Id("search-box"), search);
        return context.GetAll(By.Name("featureIds"));
    }

    /// <summary>
    /// Clicks the "Toggle" option in the "Bulk actions" dropdown, found in the Features page.
    /// </summary>
    public static Task BulkActionsToggleAsync(this UITestContext context) =>
        context.SelectFromBootstrapDropdownReliablyAsync(By.Id("bulk-action-menu-button"), "Toggle");

    /// <summary>
    /// Navigate to the Registration page.
    /// </summary>
    public static Task GoToRegistrationAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync(UserRegistrationParameters.DefaultUrl);
}
