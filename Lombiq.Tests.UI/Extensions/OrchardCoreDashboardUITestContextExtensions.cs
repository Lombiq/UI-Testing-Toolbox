using AngleSharp.Text;
using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class OrchardCoreDashboardUITestContextExtensions
    {
        public static void FillContentItemTitle(this UITestContext context, string title) =>
            context.ClickAndFillInWithRetries(By.Id("TitlePart_Title"), title);

        public static void GoToEditorTab(this UITestContext context, string tabText)
        {
            context.ClickReliablyOn(By.XPath($"//*[text()='{tabText}' and @class='nav-item nav-link']"));
            var tabId = context.Get(By.XPath("//a[contains(@class,'nav-item nav-link active')]"))
                .GetAttribute("aria-controls");

            // If there is a datatable on the tab, then wait to load all the items on the first page.
            var info = context.Get(By.XPath($"//div[contains(@id,'{tabId}')]//div[contains(@class,'dataTables_info')]").Safely());
            if (info == null) return;

            var itemsOnThePage = info.Text.Trim().Split(' ')[3].ToInteger(0);

            context.WaitUntilExists(By.XPath($"//div[contains(@id,'{tabId}')]//tbody//tr[{itemsOnThePage}]"));
        }

        public static void ClickPublish(this UITestContext context, bool withJavaScript = false)
        {
            if (withJavaScript)
            {
                context.ExecuteScript($"document.querySelector('.publish-button').click()");
            }
            else
            {
                context.ClickReliablyOn(By.Name("submit.Publish"));
            }
        }

        public static void GoToContentItemList(this UITestContext context)
        {
            context.ClickReliablyOn(By.CssSelector("#content"));
            context.ClickReliablyOn(By.LinkText("Content Items"));
        }

        public static void GoToContentItemListAndCreateNew(this UITestContext context, string contentType)
        {
            context.GoToContentItemList();
            context.ClickNewContentItem(contentType);
        }

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
