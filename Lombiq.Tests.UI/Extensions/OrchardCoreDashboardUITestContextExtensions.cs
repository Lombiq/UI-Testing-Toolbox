using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class OrchardCoreDashboardUITestContextExtensions
    {
        public static void FillContentItemTitle(this UITestContext context, string title) =>
            context.ClickAndFillInWithRetriesAsync(By.Id("TitlePart_Title"), title);

        public static void GoToEditorTab(this UITestContext context, string tabText) =>
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
        public static void ClickModalOk(this UITestContext context) => context.ClickReliablyOnAsync(By.Id("modalOkButton"));

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
        public static Task ClickPublishUntilNavigationAsync(
            this UITestContext context,
            bool withJavaScript = false,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var navigationState = context.AsPageNavigationState();

            return context.DoWithRetriesOrFailAsync(
                async () =>
                {
                    await ClickPublishAsync(context, withJavaScript);
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
            await context.ClickNewContentItemAsync(contentTypeText);
        }

        public static Task CreateNewContentItemAsync(this UITestContext context, string contentType) =>
            context.GoToRelativeUrlAsync($"/Admin/Contents/ContentTypes/{contentType}/Create");

        public static async Task ClickNewContentItemAsync(this UITestContext context, string contentItemName, bool dropdown = true)
        {
            if (dropdown)
            {
                await context.ClickReliablyOnAsync(By.Id("new-dropdown"));
                await context.ClickReliablyOnAsync(By.LinkText(contentItemName));
            }
            else
            {
                await context.ClickReliablyOnAsync(By.LinkText($"New {contentItemName}"));
            }
        }

        public static async Task GoToUsersAsync(this UITestContext context)
        {
            await context.ClickReliablyOnAsync(By.CssSelector("#security .title"));
            await context.ClickReliablyOnAsync(By.CssSelector(".item-label.users .title"));
        }
    }
}
