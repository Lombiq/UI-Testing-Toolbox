using Atata;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium;
using System;

// Using the Atata namespace because that'll surely be among the using declarations of the test. OpenQA.Selenius not
// necessarily.
namespace Lombiq.Tests.UI.Extensions
{
    public static class FormWebElementExtensions
    {
        public static void ClickAndFillInWithRetries(
            this IWebElement element,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            element.Click();
            element.FillInWithRetries(text, timeout, interval);
        }

        public static void ClickAndClear(this IWebElement element)
        {
            element.Click();
            element.Clear();
        }

        /// <summary>
        /// Fills a form field with the given text, and retries if the value doesn't stick.
        /// </summary>
        /// <remarks>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds), Displayed == Enabled == true,
        /// sometimes filling form fields still fails. Go figure!
        /// </remarks>
        public static void FillInWithRetries(
            this IWebElement element,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.DoWithRetries(() => element.FillInWith(text).GetValue() == text, timeout, interval);
    }
}
