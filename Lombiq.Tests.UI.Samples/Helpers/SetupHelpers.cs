using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Samples.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Samples.Helpers
{
    // Some logic to run the Orchard setup is here.
    public static class SetupHelpers
    {
        public static Uri RunSetup(UITestContext context)
        {
            // You should always explicitly set the window size of the browser, otherwise the size will be random based
            // on the settings of the given machine. This is especially true when running tests in headless mode. So, we
            // set it to full HD here.
            context.SetStandardBrowserSize();

            // Running the setup.
            var uri = context
                .GoToSetupPage()
                .SetupOrchardCore(
                    context,
                    new OrchardCoreSetupParameters
                    {
                        SiteName = "Lombiq's Open-Source Orchard Core Extensions - UI Testing",
                        // Note how we use a recipe just for UI testing. This is recommended so you can do some testing-
                        // specific configuration. Check it out if you're interested. Notably, it turns off CDN usage
                        // and configures culture settings to make test execution consistent regardless of the host
                        // settings.
                        // If you use a setup recipe for local development then you can execute that from this test
                        // recipe.
                        RecipeId = "Lombiq.OSOCE.Tests.recipe",
                        // Taking care to support both SQL flavors. We'll see tests using both.
                        DatabaseProvider = context.Configuration.UseSqlServer
                            ? OrchardCoreSetupPage.DatabaseType.SqlServer
                            : OrchardCoreSetupPage.DatabaseType.Sqlite,
                        // A table prefix is not really needed but this way we also check whether we've written any SQL
                        // that doesn't support prefixes.
                        TablePrefix = "OSOCE",
                        ConnectionString = context.Configuration.UseSqlServer
                            ? context.SqlServerRunningContext.ConnectionString
                            : null,
                        // Where else would we be?!
                        SiteTimeZoneValue = "Europe/Budapest",
                    })
                .PageUri
                .Value;

            // Here we make sure that the setup actually finished and we're on the homepage where the menu is visible.
            // Without this, a failing setup may only surface much later when an assertion in a test fails. Failing
            // here quickly also allows to the UI Testing Toolbox not to run all the other tests (since without a
            // working setup that would be pointless). Check out OrchardCoreSetupConfiguration.FastFailSetup if you're
            // interested how that works.
            context.Exists(By.Id("navbarResponsive"));

            return uri;
        }

        // Just a convenience method.
        public static void RunSetupAndSignInDirectly(UITestContext context, string userName = DefaultUser.UserName)
        {
            RunSetup(context);
            context.SignInDirectly(userName);
        }
    }
}
