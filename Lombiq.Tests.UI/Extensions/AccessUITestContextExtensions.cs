using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class AccessUITestContextExtensions
    {
        /// <summary>
        /// Click on the Content > ContentItems > New > <paramref name="contentType"/> button and verifies if it shows
        /// the content item editor or an error page according to expectation.
        /// </summary>
        public static void CheckContentItemCreationAccess(
            this UITestContext context,
            string contentType,
            bool hasAccess)
        {
            context.CreateNewContentItem(contentType);

            var byHasAccess = By.XPath("//div[contains(@class, 'ta-content')]/h1[starts-with(., 'New ')]");
            var byNoAccess = By.XPath("id('content')//h1[contains(., 'You do not have access to this resource.')]");

            context.CheckExistence(byHasAccess, hasAccess);
            context.CheckExistence(byNoAccess, !hasAccess);
        }

        /// <summary>
        /// Same as <see cref="CheckContentItemCreationAccess"/> but also signs in as <paramref name="userName"/>.
        /// </summary>
        public static void SignInDirectlyAndCheckContentItemCreationAccess(
            this UITestContext context,
            string userName,
            string contentType,
            bool hasAccess)
        {
            context.SignInDirectly(userName);
            context.CheckContentItemCreationAccess(contentType, hasAccess);
        }
    }
}
