using Lombiq.Tests.UI.Services;
using Microsoft.SqlServer.Management.Dmf;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class AccessUITestContextExtensions
    {
        /// <summary>
        /// Clicks on the Content > ContentItems > New > <paramref name="contentType"/> button and verifies if it shows
        /// the content item editor or an error page according to expectation.
        /// </summary>
        public static async Task CheckContentItemCreationAccessAsync(
            this UITestContext context,
            string contentType,
            bool hasAccess)
        {
            await context.CreateNewContentItemAsync(contentType);

            var byHasAccess = By.XPath("//div[contains(@class, 'ta-content')]/h1[starts-with(., 'New ')]");
            var byNoAccess = By.XPath("id('content')//h1[contains(., 'You do not have access to this resource.')]");

            context.CheckExistence(byHasAccess, hasAccess);
            context.CheckExistence(byNoAccess, !hasAccess);
        }

        /// <summary>
        /// Same as <see cref="CheckContentItemCreationAccessAsync"/> but also signs in as <paramref name="userName"/>.
        /// </summary>
        public static async Task SignInDirectlyAndCheckContentItemCreationAccessAsync(
            this UITestContext context,
            string userName,
            string contentType,
            bool hasAccess)
        {
            await context.SignInDirectlyAsync(userName);
            await context.CheckContentItemCreationAccessAsync(contentType, hasAccess);
        }

        /// <summary>
        /// Signs in and the navigates to the content item display URL with the ID of <paramref name="contentItemId"/>
        /// and checks if this causes an exception or not.
        /// </summary>
        public static async Task SignInDirectlyAndCheckContentItemDisplayAccessAsync(
            this UITestContext context,
            string userName,
            string contentItemId,
            bool hasAccess)
        {
            await context.SignInDirectlyAsync(userName);
            await context.GoToRelativeUrlAsync("/Contents/ContentItems/" + contentItemId);

            context.CheckExistence(
                By.XPath("//h1[contains(., 'You do not have access to this resource.')]"),
                !hasAccess);
        }
    }
}
