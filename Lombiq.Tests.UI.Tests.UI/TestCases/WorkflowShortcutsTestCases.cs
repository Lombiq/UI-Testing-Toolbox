using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Shortcuts;
using Shouldly;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class WorkflowShortcutsTestCases
{
    public static Task GenerateHttpEventUrlShouldWorkAsync(UITestBase fixture) => fixture
        .AddTest(async context =>
        {
            await context.EnableFeatureDirectlyAsync(ShortcutsFeatureIds.Workflows);
            await context.ExecuteRecipeDirectlyAsync("Lombiq.Tests.UI.Tests.UI.WorkflowShortcutsTests");
            (await context.GenerateHttpEventUrlAsync("testworkflow000000", "testhttpevent00000"))
                .ShouldStartWith("/workflows/Invoke?token=");
        })
        .AddConfiguration(ConfigurationHelper.DisableHtmlValidation)
        .RunAsync();
}
