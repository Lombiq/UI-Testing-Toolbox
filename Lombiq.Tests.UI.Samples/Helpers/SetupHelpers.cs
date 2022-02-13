using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Samples.Helpers
{
    // Some logic to run the Orchard setup is here.
    public static class SetupHelpers
    {
        // Specifying the recipe used for setup (it's in a const since we'll use it later too).
        // Note how we use a recipe just for UI testing. This is recommended so you can do some testing-specific
        // configuration. Check it out if you're interested. Notably, it turns off CDN usage and configures culture
        // settings to make test execution consistent regardless of the host settings.
        // If you use a setup recipe for local development then you can execute that from this test recipe.
        public const string RecipeId = "Lombiq.OSOCE.Tests";

        public static async Task<Uri> RunSetupAsync(UITestContext context)
        {
            // Running the setup.
            // OrchardCoreSetupParameters will initialize some basic settings from the context.
            var homepageUri = await context.GoToSetupPageAndSetupOrchardCoreAsync(
                new OrchardCoreSetupParameters(context)
                {
                    SiteName = "Lombiq's Open-Source Orchard Core Extensions - UI Testing",
                    RecipeId = RecipeId,
                    // A table prefix is not really needed but this way we also check whether we've written any SQL
                    // that doesn't support prefixes.
                    TablePrefix = "OSOCE",
                    // Where else would we be?!
                    SiteTimeZoneValue = "Europe/Budapest",
                });

            // Here we make sure that the setup actually finished and we're on the homepage where the menu is visible.
            // Without this, a failing setup may only surface much later when an assertion in a test fails. Failing
            // here quickly also allows to the UI Testing Toolbox not to run all the other tests (since without a
            // working setup that would be pointless). Check out OrchardCoreSetupConfiguration.FastFailSetup if you're
            // interested how that works.
            AssertSetupSuccessful(context);

            return homepageUri;
        }

        // Just a convenience method.
        public static async Task RunSetupAndSignInDirectlyAsync(UITestContext context, string userName = DefaultUser.UserName)
        {
            await RunSetupAsync(context);
            await context.SignInDirectlyAsync(userName);
        }

        // When using the Auto Setup feature (https://docs.orchardcore.net/en/dev/docs/reference/modules/AutoSetup/) you
        // don't need to run the setup like RunSetup() above. Instead, substitute the setup operation with one that just
        // opens the app and checks if the setup was successful, like you can see here.
        // Do note that this way you can't really use a different recipe for testing (that can, in addition to the dev
        // recipe, contain testing-specific content and configuration). So it's still better to not use Auto Setup for
        // test execution even if you use it for development: To achieve this, in your web app's Startup class you can
        // only conditionally run AddSetupFeatures("OrchardCore.AutoSetup"), based on  IConfiguration.IsUITesting().
        public static async Task<Uri> RunAutoSetupAsync(UITestContext context)
        {
            await context.GoToHomePageAsync();
            AssertSetupSuccessful(context);
            return context.GetCurrentUri();
        }

        private static void AssertSetupSuccessful(UITestContext context) => context.Exists(By.Id("navbar"));
    }
}
