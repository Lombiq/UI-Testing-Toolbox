using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;

// Using the Atata namespace because that'll surely be among the using declarations of the test. OpenQA.Selenium not
// necessarily.
namespace Lombiq.Tests.UI.Extensions
{
    public static class FormWebElementExtensions
    {
        public static void ClickAndFillInWithRetries(
            this IWebElement element,
            string text,
            UITestContext context,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            element.Click();
            element.FillInWithRetries(text, context, timeout, interval);
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
        /// sometimes filling form fields still fails. Go figure...
        /// </remarks>
        public static void FillInWithRetries(
            this IWebElement element,
            string text,
            UITestContext context,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            ReliabilityHelper.DoWithRetries(
                () =>
                {
                    if (text.Contains('@', StringComparison.OrdinalIgnoreCase))
                    {
                        // On some platforms, probably due to keyboard settings, the @ character can be missing from
                        // the address when entered into a textfield so we need to use JS. The following solution
                        // doesn't work: https://stackoverflow.com/a/52202594/220230.
                        // This needs to be done in addition to the standard FillInWith() as without that some forms
                        // start to behave strange and not save values.
                        new Actions(context.Driver).MoveToElement(element).SendKeys(text).Perform();
                    }
                    else
                    {
                        element.FillInWith(text);
                    }

                    return element.GetValue() == text;
                },
                timeout,
                interval);
    }
}
