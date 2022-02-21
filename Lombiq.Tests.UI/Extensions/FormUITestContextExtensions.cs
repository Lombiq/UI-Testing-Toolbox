using AngleSharp.Text;
using Atata;
using Lombiq.Tests.UI.Services;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// Using the Atata namespace because that'll surely be among the using declarations of the test. OpenQA.Selenium not
// necessarily.
namespace Lombiq.Tests.UI.Extensions
{
    public static class FormUITestContextExtensions
    {
        public static Task ClickAndFillInWithRetriesAsync(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            context.Get(by).Click();
            return context.FillInWithRetriesAsync(by, text, timeout, interval);
        }

        public static Task ClickAndFillInWithRetriesUntilNotBlankAsync(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            context.Get(by).Click();
            return context.FillInWithRetriesUntilNotBlankAsync(by, text, timeout, interval);
        }

        public static async Task ClickAndFillInWithRetriesIfNotNullOrEmptyAsync(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            if (!string.IsNullOrEmpty(text)) await ClickAndFillInWithRetriesAsync(context, by, text, timeout, interval);
        }

        public static Task ClickAndFillInTrumbowygEditorWithRetriesAsync(
            this UITestContext context,
            By by,
            string text,
            string expectedHtml = null,
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            var editorBy = by.Then(By.CssSelector(".trumbowyg-box > .trumbowyg-editor"));
            context.Get(editorBy).Click();

            expectedHtml ??= FormattableString.Invariant($"<p>{text}</p>");

            return context.ExecuteLoggedAsync(
                nameof(ClickAndFillInTrumbowygEditorWithRetriesAsync),
                $"{editorBy} - \"{text}\"",
                () => context.DoWithRetriesOrFailAsync(
                    () =>
                    {
                        TryFillElement(context, editorBy, text);

                        return Task.FromResult(context
                            .Get(by.Then(By.ClassName("trumbowyg-textarea")).OfAnyVisibility())
                            .GetValue() == expectedHtml);
                    },
                    timeout,
                    interval));
        }

        /// <summary>
        /// Uses Javascript to reinitialize the given field's EasyMDE instance and then access the internal CodeMirror
        /// editor to programmatically change the value. This is necessary, because otherwise the editor doesn't expose
        /// the CodeMirror library globally for editing the existing instance and this editor can't be filled using
        /// regular Selenium interactions either.
        /// </summary>
        public static void SetMarkdownEasyMdeWysiwygEditor(this UITestContext context, string id, string text)
        {
            var script = $@"
                /* First get rid of the existing editor instance. */
                document.querySelector('#{id} + .EasyMDEContainer').remove();
                /* Create a new one using the same call found in OC's MarkdownBodyPart-Wysiwyg.Edit.cshtml */
                var mde = new EasyMDE({{
                    element: document.getElementById('{id}'),
                    forceSync: true,
                    toolbar: mdeToolbar,
                    autoDownloadFontAwesome: false,
                }});
                /* Finally set the value programmatically. */
                mde.codemirror.setValue({JsonConvert.SerializeObject(text)});";

            context.Driver.ExecuteScript(script);
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
        /// Use <see cref="FillInWithRetriesUntilNotBlankAsync"/> instead if the field will contain a string different than
        /// what's written to it, e.g. when it applies some formatting to numbers.
        /// </para>
        /// <para>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds), Displayed == Enabled ==
        /// true, sometimes filling form fields still fails. Go figure...
        /// </para>
        /// </remarks>
        public static Task FillInWithRetriesAsync(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.ExecuteLoggedAsync(
                nameof(FillInWithRetriesAsync),
                $"{by} - \"{text}\"",
                () => context.DoWithRetriesOrFailAsync(
                    () => Task.FromResult(TryFillElement(context, by, text).GetValue() == text),
                    timeout,
                    interval));

        /// <summary>
        /// Fills a form field with the given text, and retries if the field is left blank (but doesn't check the
        /// value).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this instead of <see cref="FillInWithRetriesAsync"/> if the field will contain a string different than what's
        /// written to it, e.g. when it applies some formatting to numbers.
        /// </para>
        /// <para>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds), Displayed == Enabled ==
        /// true, sometimes filling form fields still fails. Go figure...
        /// </para>
        /// </remarks>
        public static Task FillInWithRetriesUntilNotBlankAsync(
            this UITestContext context,
            By by,
            string text,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.ExecuteLoggedAsync(
                nameof(FillInWithRetriesUntilNotBlankAsync),
                $"{by} - \"{text}\"",
                () => context.DoWithRetriesOrFailAsync(
                    () => Task.FromResult(!string.IsNullOrEmpty(TryFillElement(context, by, text).GetValue())),
                    timeout,
                    interval));

        /// <summary>
        /// Returns a value indicating whether the checkbox of <paramref name="by"/> is checked or not.
        /// </summary>
        public static bool IsElementChecked(this UITestContext context, By by) =>
            context.Get(by.OfAnyVisibility()).GetProperty("checked") == bool.TrueString;

        public static async Task SetCheckboxValueAsync(this UITestContext context, By by, bool isChecked)
        {
            var element = context.Get(by.OfAnyVisibility());
            var currentValue = element.GetProperty("checked") == bool.TrueString;
            if (currentValue != isChecked) await element.ClickReliablyAsync(context);
        }

        public static int GetIntValue(this UITestContext context, By by) =>
            context.Get(by).GetAttribute("value").ToInteger(0);

        /// <summary>
        /// Returns the title text of the currently selected tab. To avoid race conditions after page load, if the value
        /// is <paramref name="defaultTitle"/> it will retry within <paramref name="timeout"/>.
        /// </summary>
        public static async Task<string> GetSelectedTabTextAsync(
            this UITestContext context,
            string defaultTitle = "Content",
            TimeSpan? timeout = null,
            TimeSpan? interval = null)
        {
            string title = defaultTitle;

            await context.DoWithRetriesOrFailAsync(
                () =>
                {
                    title = context.Get(By.CssSelector(".nav-item.nav-link.active")).Text.Trim();
                    return Task.FromResult(title != defaultTitle);
                },
                timeout,
                interval);

            return title;
        }

        public static async Task SetDropdownAsync<T>(this UITestContext context, string selectId, T value)
            where T : Enum
        {
            await context.ClickReliablyOnAsync(By.Id(selectId));
            context.Get(By.CssSelector(FormattableString.Invariant($"#{selectId} option[value='{(int)(object)value}']"))).Click();
        }

        public static Task SetDropdownByTextAsync(this UITestContext context, string selectId, string value) =>
            SetDropdownByTextAsync(context, By.Id(selectId), value);

        public static async Task SetDropdownByTextAsync(this UITestContext context, By selectBy, string value)
        {
            await context.ClickReliablyOnAsync(selectBy);
            context.Get(selectBy).Get(By.XPath($".//option[contains(., '{value}')]")).Click();
        }

        /// <summary>
        /// Sets the value of the date picker via JavaScript and then raises the <c>change</c> event.
        /// </summary>
        public static void SetDatePicker(this UITestContext context, string id, DateTime value) =>
            context.ExecuteScript(
                FormattableString.Invariant($"document.getElementById('{id}').value = '{value:yyyy-MM-dd}';") +
                $"document.getElementById('{id}').dispatchEvent(new Event('change'));");

        public static DateTime GetDatePicker(this UITestContext context, string id) =>
            DateTime.ParseExact(
                context.Get(By.Id(id)).GetAttribute("value"),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

        /// <summary>
        /// Finds the first submit button and clicks on it reliably.
        /// </summary>
        public static void ClickReliablyOnSubmit(this UITestContext context) =>
            context.ClickReliablyOnAsync(By.CssSelector("button[type='submit']"));

        /// <summary>
        /// Finds the "Add New" button.
        /// </summary>
        public static IWebElement GetAddNewButton(this UITestContext context) =>
            context.Get(By.XPath("//button[contains(.,'Add New')]"));

        /// <summary>
        /// Opens the dropdown belonging to the "Add New" button. If <paramref name="byLocalMenuItem"/> is not <see
        /// langword="null"/> it will click on that element within the dropdown context as well.
        /// </summary>
        public static Task SelectAddNewDropdownAsync(this UITestContext context, By byLocalMenuItem = null) =>
            context.SelectFromBootstrapDropdownReliablyAsync(GetAddNewButton(context), byLocalMenuItem);

        /// <summary>
        /// Clicks on the <paramref name="dropdownButton"/> until the Bootstrap dropdown menu appears (up to 3 tries)
        /// and then clicks on the <paramref name="byLocalMenuItem"/> within the dropdown menu's context.
        /// </summary>
        /// <param name="context">The current UI test context.</param>
        /// <param name="dropdownButton">The button that reveals the Bootstrap dropdown menu.</param>
        /// <param name="byLocalMenuItem">
        /// The path inside the dropdown menu. If <see langword="null"/> then no selection (clicking) will be made, and
        /// the dropdown is left open.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if clicking on the button didn't yield a dropdown menu even after retries.
        /// </exception>
        public static async Task SelectFromBootstrapDropdownReliablyAsync(
            this UITestContext context,
            IWebElement dropdownButton,
            By byLocalMenuItem)
        {
            var byDropdownMenu = By.XPath("./following-sibling::*[contains(@class, 'dropdown-menu')]");

            for (var i = 0; i < 3; i++)
            {
                await dropdownButton.ClickReliablyAsync(context);

                var dropdownMenu = dropdownButton.GetAll(byDropdownMenu).SingleOrDefault();
                if (dropdownMenu != null)
                {
                    if (byLocalMenuItem != null) await dropdownMenu.Get(byLocalMenuItem).ClickReliablyAsync(context);
                    return;
                }
            }

            throw new InvalidOperationException($"Couldn't open dropdown menu in 3 tries.");
        }

        /// <summary>
        /// Clicks on the <paramref name="byDropdownButton"/> until the Bootstrap dropdown menu appears (up to 3 tries)
        /// and then clicks on the menu item with the <paramref name="menuItemLinkText"/> within the dropdown menu's
        /// context.
        /// </summary>
        /// <param name="context">The current UI test context.</param>
        /// <param name="byDropdownButton">The path of the button that reveals the Bootstrap dropdown menu.</param>
        /// <param name="menuItemLinkText">The text of the dropdown menu item.</param>
        public static Task SelectFromBootstrapDropdownReliablyAsync(
            this UITestContext context,
            By byDropdownButton,
            string menuItemLinkText) =>
            SelectFromBootstrapDropdownReliablyAsync(context, context.Get(byDropdownButton), By.LinkText(menuItemLinkText));

        private static IWebElement TryFillElement(UITestContext context, By by, string text)
        {
            var element = context.Get(by);

            return context.Driver.TryFillElement(element, text);
        }
    }
}
