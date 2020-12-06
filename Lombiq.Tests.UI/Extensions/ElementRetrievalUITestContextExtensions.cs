using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ElementRetrievalUITestContextExtensions
    {
        /// <summary>
        /// Retrieves the matching element with retries within the configured timeout.
        /// </summary>
        public static IWebElement Get(this UITestContext context, By by) => context.Driver.Get(by);

        /// <summary>
        /// Retrieves all the matching elements with retries within the configured timeout. Don't use this for
        /// existence check, use <see cref="CheckExistence(UITestContext, By, bool)"/>, <see
        /// cref="Exists(UITestContext, By)"/>, and <see cref="Missing(UITestContext, By)"/> instead.
        /// </summary>
        public static ReadOnlyCollection<IWebElement> GetAll(this UITestContext context, By by) => context.Driver.GetAll(by);

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
        public static bool Exists(this UITestContext context, By by) => context.Driver.Exists(by);

        /// <summary>
        /// Checks the existence of the element with retries within the configured timeout. Depending on the
        /// configuration of <paramref name="by"/> will return a value indicating whether the element is missing or will
        /// throw an exception if it doesn't. For details see <see
        /// href="https://github.com/atata-framework/atata-webdriverextras#usage"/>.
        /// </summary>
        public static bool Missing(this UITestContext context, By by) => context.Driver.Missing(by);
    }
}
