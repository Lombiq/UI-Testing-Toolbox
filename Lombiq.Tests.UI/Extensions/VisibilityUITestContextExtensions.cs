using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    public static class VisibilityUITestContextExtensions
    {
        /// <summary>
        /// Make the native checkboxes visible on the admin so they can be selected and Selenium operations can work on
        /// them as usual. This is necessary because the Orchard admin theme uses custom controls which hide the native
        /// <c>&lt;input&gt;</c> elements by setting their opacity to 0. Thus they're inaccessible to Selenium unless
        /// they're revealed like this. Once interactions with these elements are done it's good practice to revert this
        /// change with <see cref="RevertAdminCheckboxesVisibility(UITestContext)"/>.
        /// </summary>
        public static void MakeAdminCheckboxesVisible(this UITestContext context) =>
            context.ExecuteScript("Array.from(document.querySelectorAll('.custom-control-input')).forEach(x => x.style.opacity = 1)");

        /// <summary>
        /// Reverts the visibility of admin checkboxes made visible with <see
        /// cref="MakeAdminCheckboxesVisible(UITestContext)"/>.
        /// </summary>
        public static void RevertAdminCheckboxesVisibility(this UITestContext context) =>
            context.ExecuteScript("Array.from(document.querySelectorAll('.custom-control-input')).forEach(x => x.style.opacity = 0)");
    }
}
