using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Provides extension methods for testing themes' UI.
    /// </summary>
    public static class ThemeUITestContextExtensions
    {
        /// <summary>
        /// Goes to the home page and checks if the site name is correct, and the credits are visible.
        /// </summary>
        /// <param name="context">The context of the currently executed UI test.</param>
        /// <param name="siteName">The name of the current site.</param>
        /// <param name="creditsClass">CSS class name of the credits HTML element.</param>
        /// <param name="siteNameClass">CSS class name of the HTML element that has the site name text inside.</param>
        public static async Task GoToHomePageAndCheckNavBarAndCreditsAsync(
            this UITestContext context,
            string siteName = "Test Site",
            string creditsClass = "credits",
            string siteNameClass = "navbar-brand")
        {
            await context.GoToHomePageAsync();

            context.Get(By.ClassName(siteNameClass)).Text.ShouldBe(siteName);
            context.Driver.Exists(By.ClassName(creditsClass).Visible());
        }
    }
}
