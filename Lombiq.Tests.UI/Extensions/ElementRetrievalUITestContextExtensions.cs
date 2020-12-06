using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ElementRetrievalUITestContextExtensions
    {
        public static IWebElement Get(this UITestContext context, By by) => context.Driver.Get(by);

        public static ReadOnlyCollection<IWebElement> GetAll(this UITestContext context, By by) => context.Driver.GetAll(by);

        public static bool CheckExistence(this UITestContext context, By by, bool exists) =>
            exists ? context.Exists(by) : context.Missing(by);

        public static bool Exists(this UITestContext context, By by) => context.Driver.Exists(by);

        public static bool Missing(this UITestContext context, By by) => context.Driver.Missing(by);
    }
}
