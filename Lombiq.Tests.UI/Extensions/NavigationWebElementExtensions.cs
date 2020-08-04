using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class NavigationWebElementExtensions
    {
        /// <summary>
        /// Click an element even if the default Click() will sometimes fail to do so. It's more reliable than Click()
        /// but still not perfect.
        /// </summary>
        /// <remarks>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds) the clicks sometimes simply
        /// don't go through the first time.
        /// More literature on the scientific field of clicking (but the code there doesn't really help):
        /// https://cezarypiatek.github.io/post/why-click-with-selenium-so-hard/
        /// Also see: https://stackoverflow.com/questions/11908249/debugging-element-is-not-clickable-at-point-error.
        /// </remarks>
        public static void ClickReliably(this IWebElement element, UITestContext context) => element.ClickReliably(context.Driver);

        public static void ClickReliably(this IWebElement element, IWebDriver driver)
        {
            try
            {
                new Actions(driver).MoveToElement(element).Click().Perform();
            }
            catch (WebDriverException ex)
                when (ex.Message.Contains("javascript error: Failed to execute 'elementsFromPoint' on 'Document': The provided double value is non-finite."))
            {
                throw new NotSupportedException(
                    "For this element use the standard Click() method. Add the element as an exception to the documentation.");
            }
        }

        public static void ClickReliablyUntilPageLeave(
            this IWebElement element,
            UITestContext context,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            element.ClickReliablyUntilPageLeave(context.Driver, timeout, interval);

        public static void ClickReliablyUntilPageLeave(
            this IWebElement element,
            IWebDriver driver,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.DoWithRetries(() =>
            {
                try
                {
                    element.ClickReliably(driver);
                    return false;
                }
                catch (StaleElementReferenceException)
                {
                    // When navigating away this exception will be thrown for all old element references. Not nice but
                    // there doesn't seem to be a better way to do this.
                    return true;
                }
            }, timeout, interval);

        public static void ClickNewContentItem(this UITestContext context, string contentItemName)
        {
            context.Get(By.Id("new-dropdown")).ClickReliably(context);
            context.Get(By.LinkText(contentItemName)).ClickReliably(context);
        }

        public static void GoToUsers(this UITestContext context)
        {
            context.Get(By.CssSelector("#security .title")).ClickReliably(context);
            context.Get(By.CssSelector(".item-label.users .title")).ClickReliably(context);
        }
    }
}
