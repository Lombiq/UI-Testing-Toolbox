using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Collections.ObjectModel;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Extension methods to retrieve elements using Atata helpers.
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

        private static ExtendedSearchContext<RemoteWebDriver> CreateSearchContext(this UITestContext context) =>
            new ExtendedSearchContext<RemoteWebDriver>(
                context.Driver,
                context.Configuration.TimeoutConfiguration.RetryTimeout,
                context.Configuration.TimeoutConfiguration.RetryTimeout);
    }
}
