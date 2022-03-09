using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ScreenshotUITestContextExtensions
    {
        /// <summary>
        /// Takes a screenshot of the current browser tab and saves it under the given path.
        /// </summary>
        public static void TakeScreenshot(this UITestContext context, string imagePath) =>
            context.TakeScreenshot().SaveAsFile(imagePath);

        /// <summary>
        /// Takes a screenshot of the current browser tab.
        /// </summary>
        public static Screenshot TakeScreenshot(this UITestContext context) =>
            context.Scope.Driver.GetScreenshot();
    }
}
