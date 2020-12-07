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
        public static IWebElement Get(this UITestContext context, By by) =>
            context.CreateSearchContext().FindElement(by);

        public static ReadOnlyCollection<IWebElement> GetAll(this UITestContext context, By by) =>
            context.CreateSearchContext().FindElements(by);

        public static bool Exists(this UITestContext context, By by) => context.CreateSearchContext().Exists(by);

        public static bool Missing(this UITestContext context, By by) => context.CreateSearchContext().Missing(by);

        private static ExtendedSearchContext<RemoteWebDriver> CreateSearchContext(this UITestContext context) =>
            new ExtendedSearchContext<RemoteWebDriver>(
                context.Driver,
                context.Configuration.TimeoutConfiguration.RetryTimeout,
                context.Configuration.TimeoutConfiguration.RetryTimeout);
    }
}
