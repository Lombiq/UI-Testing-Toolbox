using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class OrchardCoreDashboardUITestContextExtensions
    {
        public static void FillContentItemTitle(this UITestContext context, string title) =>
            context.Get(By.Id("TitlePart_Title")).ClickAndFillInWithRetries(title);

        public static void GoToEditorTab(this UITestContext context, string tabText) =>
            context.Get(By.XPath($"//*[text()='{tabText}' and @class='nav-item nav-link']")).ClickReliably(context);

        public static void ClickPublish(this UITestContext context) =>
            context.Get(By.Name("submit.Publish")).ClickReliably(context);

        public static void GoToContentItemList(this UITestContext context)
        {
            context.Get(By.CssSelector("#content")).ClickReliably(context);
            context.Get(By.LinkText("Content Items")).ClickReliably(context);
        }

        public static void GoToContentItemListAndCreateNew(this UITestContext context, string contentType)
        {
            context.GoToContentItemList();
            context.ClickNewContentItem(contentType);
        }

        public static void ClickNewContentItem(this UITestContext context, string contentType)
        {
            context.Get(By.Id("new-dropdown")).ClickReliably(context);
            context.Get(By.LinkText(contentType)).ClickReliably(context);
        }

        public static void GoToUsers(this UITestContext context)
        {
            context.Get(By.CssSelector("#security .title")).ClickReliably(context);
            context.Get(By.CssSelector(".item-label.users .title")).ClickReliably(context);
        }
    }
}
