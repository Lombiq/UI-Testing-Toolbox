using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class CustomAdminPrefixTestCases
{
    public static Task NavigationWithCustomAdminPrefixShouldWorkAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync, Browser browser) =>
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

                return Task.CompletedTask;
            });
}
