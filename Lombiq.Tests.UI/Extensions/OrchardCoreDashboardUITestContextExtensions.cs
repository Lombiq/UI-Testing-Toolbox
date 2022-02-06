using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class OrchardCoreDashboardUITestContextExtensions
    {
        public static void FillContentItemTitle(this UITestContext context, string title) =>
            context.ClickAndFillInWithRetries(By.Id("TitlePart_Title"), title);

        public static void GoToEditorTab(this UITestContext context, string tabText) =>
            context.ClickReliablyOn(By.XPath($"//*[text()='{tabText}' and @class='nav-item nav-link']"));

        public static void ClickPublish(this UITestContext context, bool withJavaScript = false)
        {
            if (withJavaScript)
            {
                context.ExecuteScript("document.querySelector('.publish-button, .publish.btn').click();");
            }
            else
            {
                context.ClickReliablyOn(By.Name("submit.Publish"));
            }
        }

        /// <summary>
        /// Clicks on the "Ok" button on the Bootstrap modal window.
        /// </summary>
        public static void ClickModalOk(this UITestContext context) => context.ClickReliablyOn(By.Id("modalOkButton"));

        /// <summary>
        /// Sometimes the Publish button doesn't get clicked. This method retries pressing it up to 4 times with a 30
        /// second interval between attempts. This should grant enough time to execute the publish action if the button
        /// actually got pressed.
        /// </summary>
        /// <remarks><para>
        /// The <paramref name="timeout"/> and <paramref name="interval"/> have different default values from other
        /// similar methods that get theirs from the test configuration. These defaults are set to minimize the chance
        /// of an unintended early timeout or bounce effect because the publishing may take a longer time.
        /// </para></remarks>
        public static void ClickPublishUntilNavigation(
            this UITestContext context,
            bool withJavaScript = false,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var navigationState = context.AsPageNavigationState();

            context.DoWithRetriesOrFail(
                () =>
                {
                    ClickPublish(context, withJavaScript);
                    return navigationState.CheckIfNavigationHasOccurred();
                },
                timeout ?? TimeSpan.FromSeconds(30),
                interval ?? TimeSpan.FromMinutes(2));
        }

        public static Task GoToContentItemListAsync(this UITestContext context, string filterContentType = null)
        {
            var query = string.IsNullOrEmpty(filterContentType)
                ? string.Empty
                : ("?q=type%3A" + filterContentType);
            return context.GoToRelativeUrlAsync("/Admin/Contents/ContentItems" + query);
        }

        public static async Task GoToContentItemListAndCreateNewAsync(this UITestContext context, string contentTypeText)
        {
            await context.GoToContentItemListAsync();
            context.ClickNewContentItem(contentTypeText);
        }

        public static Task CreateNewContentItemAsync(this UITestContext context, string contentType) =>
            context.GoToRelativeUrlAsync($"/Admin/Contents/ContentTypes/{contentType}/Create");

        public static void ClickNewContentItem(this UITestContext context, string contentItemName, bool dropdown = true)
        {
            if (dropdown)
            {
                context.ClickReliablyOn(By.Id("new-dropdown"));
                context.ClickReliablyOn(By.LinkText(contentItemName));
            }
            else
            {
                context.ClickReliablyOn(By.LinkText($"New {contentItemName}"));
            }
        }

        public static void GoToUsers(this UITestContext context)
        {
            context.ClickReliablyOn(By.CssSelector("#security .title"));
            context.ClickReliablyOn(By.CssSelector(".item-label.users .title"));
        }
    }
}
