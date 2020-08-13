using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Drawing;

namespace Lombiq.Tests.UI.Extensions
{
    public static class VisibilityUITestContextExtensions
    {
        /// <summary>
        /// Make the native checkboxes visible on the admin so they can be selected and Selenium operations can work on
        /// them as usual. This is necessary because the Orchard admin theme uses custom controls which hide the native
        /// <c>&lt;input&gt;</c> elements by setting their opacity to 0. Thus they're inaccessible to Selenium unless they're
        /// revealed like this. Once interactions with these elements are done it's good practice to revert this change
        /// with <see cref="RevertAdminCheckboxesVisibility(UITestContext)"/>.
        /// </summary>
        public static void MakeAdminCheckboxesVisible(this UITestContext context) =>
            (context.Driver as IJavaScriptExecutor)
            .ExecuteScript("Array.from(document.querySelectorAll('.custom-control-input')).forEach(x => x.style.opacity = 1)");

        /// <summary>
        /// Reverts the visibility of admin checkboxes made visible with
        /// <see cref="MakeAdminCheckboxesVisible(UITestContext)"/>.
        /// </summary>
        public static void RevertAdminCheckboxesVisibility(this UITestContext context) =>
            (context.Driver as IJavaScriptExecutor)
            .ExecuteScript("Array.from(document.querySelectorAll('.custom-control-input')).forEach(x => x.style.opacity = 0)");

        /// <summary>
        /// Set the browser window's size to the given value. See <see
        /// cref="Lombiq.Tests.UI.Constants.CommonDisplayResolutions"/> for some resolution presets (but generally it's
        /// better to test the given app's responsive breakpoints specifically).
        /// </summary>
        /// <remarks>
        /// Note that if you switch windows/tabs during the test you may need to set the browser size again.
        /// </remarks>
        /// <param name="size">The outer size of the browser window.</param>
        public static void SetBrowserSize(this UITestContext context, Size size)
        {
            context.Scope.AtataContext?.Log.Info($"Set window size to {size.Width}x{size.Height}.");
            context.Driver.Manage().Window.Size = size;
        }
    }
}
