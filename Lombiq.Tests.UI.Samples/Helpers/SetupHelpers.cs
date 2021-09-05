using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Samples.Helpers
{
    public static class SetupHelpers
    {
        public static Uri RunSetup(UITestContext context)
        {
            var uri = context
                .GoToSetupPage()
                .SetupOrchardCore(
                    context,
                    new OrchardCoreSetupParameters
                    {
                        SiteName = "Lombiq's Open-Source Orchard Core Extensions - UI Testing",
                        RecipeId = "Lombiq.OSOCE.Tests.recipe",
                        DatabaseProvider = context.Configuration.UseSqlServer
                            ? OrchardCoreSetupPage.DatabaseType.SqlServer
                            : OrchardCoreSetupPage.DatabaseType.Sqlite,
                        // A table prefix is not really needed but this way we also check whether we've written any SQL
                        // that doesn't support prefixes.
                        TablePrefix = "OSOCE",
                        ConnectionString = context.Configuration.UseSqlServer
                            ? context.SqlServerRunningContext.ConnectionString
                            : null,
                        SiteTimeZoneValue = "Europe/Budapest",
                    })
                .PageUri
                .Value;

            // Making sure the setup actually finished and we're on the homepage where the menu is visible.
            context.Exists(By.Id("navbarResponsive"));

            return uri;
        }

        public static void SetupFinitivePlatformAndSignInDirectly(UITestContext context, string userName = DefaultUser.UserName)
        {
            RunSetup(context);
            context.SignInDirectly(userName);
        }
    }
}
