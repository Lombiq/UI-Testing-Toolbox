using AngleSharp.Text;
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
    public static class FormUITestContextExtensions
    {
        public static void ClickAndFillInWithRetries(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            context.Get(by).Click();
            context.FillInWithRetries(by, text, timeout, interval);
        }

        public static void ClickAndFillInWithRetriesUntilNotBlank(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            context.Get(by).Click();
            context.FillInWithRetriesUntilNotBlank(by, text, timeout, interval);
        }

        public static void ClickAndFillInWithRetriesIfNotNullOrEmpty(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            if (!string.IsNullOrEmpty(text)) ClickAndFillInWithRetries(context, by, text, timeout, interval);
        }

        public static void ClickAndFillInTrumbowygEditorWithRetries(
            this UITestContext context,
            string editorContainerId,
            string text,
            string expectedHtml,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var editorBy = By.CssSelector($"#{editorContainerId} > .trumbowyg-box > .trumbowyg-editor");
            context.Get(editorBy).Click();

            context.ExecuteLogged(
                nameof(ClickAndFillInTrumbowygEditorWithRetries),
                $"{editorBy} - \"{text}\"",
                () => ReliabilityHelper.DoWithRetriesOrFail(
                    () =>
                    {
                        TryFillElement(context, editorBy, text);

                        return context
                            .Get(By.CssSelector($"#{editorContainerId} .trumbowyg-textarea").OfAnyVisibility())
                            .GetValue() == expectedHtml;
                    },
                    timeout,
                    interval));
        }

        public static void ClickAndClear(this UITestContext context, By by) =>
            context.ExecuteLogged(
                nameof(ClickAndClear),
                by,
                () =>
                {
                    var element = context.Get(by);
                    element.Click();
                    element.Clear();
                });

        /// <summary>
        /// Fills a form field with the given text, and retries if the value doesn't stick.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use <see cref="FillInWithRetriesUntilNotBlank"/> instead if the field will contain a string different than
        /// what's written to it, e.g. when it applies some formatting to numbers.
        /// </para>
        /// <para>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds), Displayed == Enabled ==
        /// true, sometimes filling form fields still fails. Go figure...
        /// </para>
        /// </remarks>
        public static void FillInWithRetries(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.ExecuteLogged(
                nameof(FillInWithRetries),
                $"{by} - \"{text}\"",
                () => ReliabilityHelper.DoWithRetriesOrFail(
                    () => TryFillElement(context, by, text).GetValue() == text,
                    timeout,
                    interval));

        /// <summary>
        /// Fills a form field with the given text, and retries if the field is left blank (but doesn't check the
        /// value).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this instead of <see cref="FillInWithRetries"/> if the field will contain a string different than what's
        /// written to it, e.g. when it applies some formatting to numbers.
        /// </para>
        /// <para>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds), Displayed == Enabled ==
        /// true, sometimes filling form fields still fails. Go figure...
        /// </para>
        /// </remarks>
        public static void FillInWithRetriesUntilNotBlank(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.ExecuteLogged(
                nameof(FillInWithRetriesUntilNotBlank),
                $"{by} - \"{text}\"",
                () => ReliabilityHelper.DoWithRetriesOrFail(
                    () => !string.IsNullOrEmpty(TryFillElement(context, by, text).GetValue()),
                    timeout,
                    interval));

        /// <summary>
        /// Returns a value indicating whether the checkbox of <paramref name="by"/> is checked or not.
        /// </summary>
        public static bool IsElementChecked(this UITestContext context, By by) =>
            context.Get(by.OfAnyVisibility()).GetProperty("checked") == bool.TrueString;

        public static void SetCheckboxValue(this UITestContext context, By by, bool isChecked)
        {
            var element = context.Get(by.OfAnyVisibility());
            var currentValue = element.GetProperty("checked") == bool.TrueString;
            if (currentValue != isChecked) element.ClickReliably(context);
        }

        public static int GetIntValue(this UITestContext context, By by) =>
            context.Get(by).GetAttribute("value").ToInteger(0);

        /// <summary>
        /// Returns the title text of the currently selected tab. To avoid race conditions after page load, if the value
        /// is <paramref name="defaultTitle"/> it will retry within <paramref name="timeout"/> using
        /// <see cref="ReliabilityHelper.DoWithRetries"/>.
        /// </summary>
        public static string GetSelectedTabText(
            this UITestContext context,
            string defaultTitle = "Content",
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            string title = defaultTitle;

            ReliabilityHelper.DoWithRetriesOrFail(
                () =>
                {
                    title = context.Get(By.CssSelector(".nav-item.nav-link.active")).Text.Trim();
                    return title != defaultTitle;
                },
                timeout,
                interval);

            return title;
        }

        private static IWebElement TryFillElement(UITestContext context, By by, string text)
        {
            var element = context.Get(by);

            if (text.Contains('@', StringComparison.OrdinalIgnoreCase))
            {
                element.Clear();

                // On some platforms, probably due to keyboard settings, the @ character can be missing from the address
                // when entered into a textfield so we need to use JS. The following solution doesn't work:
                // https://stackoverflow.com/a/52202594/220230. This needs to be done in addition to the standard
                // FillInWith() as without that some forms start to behave strange and not save values.
                new Actions(context.Driver).SendKeys(element, text).Perform();
            }
            else
            {
                element.FillInWith(text);
            }

            return element;
        }
    }
}
