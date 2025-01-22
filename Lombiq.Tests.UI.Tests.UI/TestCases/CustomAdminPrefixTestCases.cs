using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class CustomAdminPrefixTestCases
{
    public static Task NavigationWithCustomAdminPrefixShouldWorkAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync, Browser browser = default) =>
        executeTestAfterSetupAsync(
            async context =>
            {
                context.AdminUrlPrefix = "custom-admin";

                await context.SignInDirectlyAsync();
                await context.GoToDashboardAsync();
                await context.GoToFeaturesPageAsync();
                await context.GoToContentItemListAsync("Blog");
                await context.GoToContentItemsPageAsync();
            },
            browser,
            configuration =>
            {
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;

                configuration.OrchardCoreConfiguration.BeforeAppStart += (_, argsBuilder) =>
                {
                    argsBuilder.AddWithValue("OrchardCore:OrchardCore_Admin:AdminUrlPrefix", "custom-admin");

                    return Task.CompletedTask;
                };

                // Using a custom setup operation so UITT will consider it unique, and not reuse its snapshot with other
                // tests if this one ends up running first. This is necessary because the AdminUrlPrefix is saved to the
                // DB by the OrchardCore.AdminMenu feature, like for the admin menu's Blog item; we don't want the other
                // tests to use that.
                var originalSetupOperation = configuration.SetupConfiguration.SetupOperation;
                configuration.SetupConfiguration.SetupOperation = context => originalSetupOperation(context);

                return Task.CompletedTask;
            });
}
