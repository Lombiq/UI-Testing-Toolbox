using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

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

        public static void ClickModalOk(this UITestContext context) => context.ClickReliablyOn(By.Id("modalOkButton"));

        /// <summary>
        /// Sometimes the Publish button doesn't get clicked. This method retries pressing it up to 4 times with a 30
        /// second interval between attempts. This should grant enough time to execute the publish action if the button
        /// actually got pressed.
        /// </summary>
        public static void ClickPublishReliably(this UITestContext context, bool withJavaScript = false)
        {
            var navigationState = context.AsPageNavigationState();

            context.DoWithRetriesOrFail(
                () =>
                {
                    ClickPublish(context, withJavaScript);
                    return navigationState.CheckIfNavigationHasOccurred();
                },
                timeout: TimeSpan.FromSeconds(30),
                interval: TimeSpan.FromMinutes(2));
        }

        public static void GoToContentItemList(this UITestContext context) =>
            context.GoToRelativeUrl("/Admin/Contents/ContentItems");

        public static void GoToContentItemListAndCreateNew(this UITestContext context, string contentTypeText)
        {
            context.GoToContentItemList();
            context.ClickNewContentItem(contentTypeText);
        }

        public static void CreateNewContentItem(this UITestContext context, string contentType) =>
            context.GoToRelativeUrl($"/Admin/Contents/ContentTypes/{contentType}/Create");

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
