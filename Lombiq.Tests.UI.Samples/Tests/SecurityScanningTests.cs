using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Note that security scanning has cross-platform support, but due to the limitations of virtualization under Windows in
// GitHub Actions, these tests won't work there. They'll work on a Windows desktop though.
public class SecurityScanningTests : UITestBase
{
    public SecurityScanningTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task SecurityScanShouldPass(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.ZapManager.RunSecurityScanAsync(context, context.Scope.BaseUri);
            },
            browser);

    // Overriding the default setup so we can have a simpler site, simplifying the security scan for the purpose of this
    // demo.
    protected override Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(
            testAsync,
            browser,
            async context =>
            {
                var homepageUri = await context.GoToSetupPageAndSetupOrchardCoreAsync(
                    new OrchardCoreSetupParameters(context)
                    {
                        SiteName = "Lombiq's OSOCE - UI Testing",
                        // We can't use the even simpler Coming Soon recipe due to this ZAP bug:
                        // https://github.com/zaproxy/zaproxy/issues/8191.
                        RecipeId = "Blog",
                        TablePrefix = "OSOCE",
                        SiteTimeZoneValue = "Europe/Budapest",
                    });

                context.Exists(By.ClassName("site-heading"));

                return homepageUri;
            },
            async configuration =>
            {
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                await changeConfigurationAsync(configuration);
            });
}

// END OF TRAINING SECTION: Security scanning.
