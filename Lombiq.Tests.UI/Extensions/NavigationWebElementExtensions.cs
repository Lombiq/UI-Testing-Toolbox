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
        /// A convenience method that merges <see cref="ElementRetrievalUITestContextExtensions.Get"/> and
        /// <see cref="ClickReliably(OpenQA.Selenium.IWebElement,Lombiq.Tests.UI.Services.UITestContext)"/> so the
        /// <paramref name="context"/> doesn't have to be passed twice.
        /// </summary>
        public static void ClickReliablyOn(this UITestContext context, By by) => ClickReliably(context.Get(by), context);

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
                when (ex.Message.Contains(
                    "javascript error: Failed to execute 'elementsFromPoint' on 'Document': The provided double value is non-finite.",
                    StringComparison.InvariantCultureIgnoreCase))
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
            ReliabilityHelper.DoWithRetries(
                () =>
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
                },
                timeout,
                interval);

        public static void SetDropdown<T>(this UITestContext context, string selectId, T value)
            where T : Enum
        {
            context.Get(By.Id(selectId)).ClickReliably(context);
            context.Get(By.CssSelector($"#{selectId} option[value='{(int)(object)value}']")).Click();
        }

        public static void SetDatePicker(this UITestContext context, string id, DateTime value) =>
            ((IJavaScriptExecutor)context.Driver).ExecuteScript(
                $"document.getElementById('{id}').value = '{value:yyyy-MM-dd}';");
    }
}
