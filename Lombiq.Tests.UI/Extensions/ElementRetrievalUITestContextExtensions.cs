using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Extension methods to retrieve elements using Atata helpers. See the Atata docs
    /// (<see href="https://github.com/atata-framework/atata-webdriverextras#usage"/>) for more information on what you
    /// can do with these.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ISearchContextExtensions"/> from Atata.WebDriverExtras can't be directly used because that wouldn't
    /// use our timeout configurations. Thus using the methods that it uses.
    /// </para>
    /// </remarks>
    public static class ElementRetrievalUITestContextExtensions
    {
        /// <summary>
        /// Retrieves the matching element with retries within the configured timeout.
        /// </summary>
        public static IWebElement Get(this UITestContext context, By by) =>
            context.ExecuteLogged(nameof(Get), by, () => context.CreateSearchContext().FindElement(by));

        /// <summary>
        /// Retrieves all the matching elements with retries within the configured timeout. Don't use this for
        /// existence check, use <see cref="CheckExistence(UITestContext, By, bool)"/>, <see
        /// cref="Exists(UITestContext, By)"/>, and <see cref="Missing(UITestContext, By)"/> instead.
        /// </summary>
        public static ReadOnlyCollection<IWebElement> GetAll(this UITestContext context, By by) =>
            context.ExecuteLogged(nameof(GetAll), by, () => context.CreateSearchContext().FindElements(by));

        /// <summary>
        /// Conditionally checks the existence of the element with retries within the configured timeout.
        /// </summary>
        public static bool CheckExistence(this UITestContext context, By by, bool exists) =>
            exists ? context.Exists(by) : context.Missing(by);

        /// <summary>
        /// Checks the existence of the element with retries within the configured timeout. Depending on the
        /// configuration of <paramref name="by"/> will return a value indicating whether the element exists or will
        /// throw an exception if it doesn't. For details see <see
        /// href="https://github.com/atata-framework/atata-webdriverextras#usage"/>.
        /// </summary>
        public static bool Exists(this UITestContext context, By by) =>
            context.ExecuteLogged(nameof(Exists), by, () => context.CreateSearchContext().Exists(by));

        /// <summary>
        /// Checks the existence of the element with retries within the configured timeout. Depending on the
        /// configuration of <paramref name="by"/> will return a value indicating whether the element is missing or will
        /// throw an exception if it doesn't. For details see <see
        /// href="https://github.com/atata-framework/atata-webdriverextras#usage"/>.
        /// </summary>
        public static bool Missing(this UITestContext context, By by) =>
            context.ExecuteLogged(nameof(Missing), by, () => context.CreateSearchContext().Missing(by));

        /// <summary>
        /// Verifies that the current page doesn't show any validation error notifications.
        /// </summary>
        public static void ShouldHaveNoValidationErrors(this UITestContext context) =>
            context.Missing(By.CssSelector(".validation-summary-errors li"));

        /// <summary>
        /// Verifies that publishing a content item has succeeded. No warning or error messages are allowed.
        /// </summary>
        /// <param name="matchText">If not <see langword="null"/> or empty, the element should contain its value.</param>
        /// <param name="within">If not <see langword="null"/>, the element will be searched for that long.</param>
        public static void ShouldBeSuccess(this UITestContext context, string matchText = null, TimeSpan? within = null)
        {
            context.SucccessMessageExists(matchText, within);

            context.Missing(By.CssSelector(".message-warning"));
            context.Missing(By.CssSelector(".message-error"));
        }

        /// <summary>
        /// Verifies that publishing a content item has succeeded, where warning or error messages are allowed to show.
        /// </summary>
        /// <param name="matchText">If not <see langword="null"/> or empty, the element should contain its value.</param>
        /// <param name="within">If not <see langword="null"/>, the element will be searched for that long.</param>
        public static void SucccessMessageExists(this UITestContext context, string matchText = null, TimeSpan? within = null)
        {
            var by = By.CssSelector(".message-success");
            if (within is { } timeSpan) by = by.Within(timeSpan);

            var element = context.Get(by);
            if (!string.IsNullOrEmpty(matchText)) element.Text.Trim().ShouldContain(matchText);
        }

        /// <summary>
        /// Check if error message is shown.
        /// </summary>
        /// <param name="errorMessage">Error message to look for.</param>
        public static void ErrorMessageExists(this UITestContext context, string errorMessage) =>
            context.Get(By.CssSelector(".validation-summary-errors li"))
                .Text
                .ShouldBe(errorMessage);

        /// <summary>
        /// Retrieves the elements according to <paramref name="by"/> and matches their text content against <paramref
        /// name="toMatch"/>. Both the text contents and <paramref name="toMatch"/> strings are trimmed. If an item in
        /// <paramref name="toMatch"/> is <see langword="null" /> it's ignored among the result elements too. Every
        /// other item is converted to string, using invariant culture where possible.
        /// </summary>
        public static void VerifyElementTexts(this UITestContext context, By by, params object[] toMatch)
        {
            context.Exists(by); // Ensure content is loaded first.

            var dontCare = toMatch
                .Select((item, index) => item == null ? index : -1)
                .Where(index => index >= 0)
                .ToList();
            var target = toMatch
                .Select(item => item == null ? null : FormattableString.Invariant($"{item}"))
                .Select(item => item?.Trim())
                .ToArray();

            context
                .GetAll(by)
                .Select((element, index) => dontCare.Contains(index) ? null : element.Text.Trim())
                .ToArray()
                .ShouldBe(target);
        }

        /// <inheritdoc cref="VerifyElementTexts(UITestContext, By, object[])"/>
        public static void VerifyElementTexts(this UITestContext context, By by, IEnumerable<object> toMatch) =>
            VerifyElementTexts(context, by, toMatch is object[] array ? array : toMatch.ToArray());

        private static ExtendedSearchContext<RemoteWebDriver> CreateSearchContext(this UITestContext context) =>
            new(
                context.Driver,
                context.Configuration.TimeoutConfiguration.RetryTimeout,
                context.Configuration.TimeoutConfiguration.RetryTimeout);
    }
}
