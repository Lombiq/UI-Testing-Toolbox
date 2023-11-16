using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) is the world's most widely used web app security scanner, and a
// fellow open-source project we can recommend. And you can use it right from UI tests, on the same app that's run for
// the tests! This is useful to find all kinds of security issues with your app. In this sample we'll see how, but be
// sure to also check out the corresponding documentation page:
// https://github.com/Lombiq/UI-Testing-Toolbox/blob/dev/Lombiq.Tests.UI/Docs/SecurityScanning.md.

// Note that security scanning has cross-platform support, but due to the limitations of virtualization under Windows in
// GitHub Actions, these tests won't work there. They'll work on a Windows desktop though.
public class SecurityScanningTests : UITestBase
{
    public SecurityScanningTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Let's see simple use case first: Running a built-in ZAP scan.

    // We're running one of ZAP's built-in scans, the Baseline scan. This, as the name suggests, provides some
    // rudimentary security checks. While you can start with this, we recommend running the Full Scan, for which there
    // similarly is an extension method as well.

    // If you're new to security scanning, starting with exactly this is probably a good idea. Most possibly your app
    // will fail the scan, but don't worry! You'll get a nice report about the findings in the failure dump.
    [Theory, Chrome]
    public Task BasicSecurityScanShouldPass(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context => await context.RunAndAssertBaselineSecurityScanAsync(),
            browser);

    // Time for some custom configuration! While this scan also runs the Baseline scan, it does this with several
    // adjustments:
    // - The assertion on the scan results is custom. Use this if you (conditionally) want to assert on the results
    //   differently from the default "no scanning alert is allowed" assertion.
    // - The plan is modified to also run ZAP's Ajax Spider
    //   (https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/). This is usually not just unnecessary for
    //   a website that's not an SPA, but also slows the scan down by a lot.
    // - The plan is also modified with an exclusion regex pattern. You can use this to exclude certain URLs from the
    //   scan.
    // - We disable the "Server Leaks Information via "X-Powered-By" HTTP Response Header Field(s)" alert of ZAP. This
    //   is because by default, Orchard Core sends an "X-Powered-By: OrchardCore" header. If you want airtight security,
    //   you might want to turn this off, but for the sake of example we just ignore the alert here.
    [Theory, Chrome]
    public Task SecurityScanWithCustomConfigurationShouldPass(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context => await context.RunAndAssertBaselineSecurityScanAsync(
                assertSecurityScanResult: sarifLog => sarifLog.Runs[0].Results.Count.ShouldBeLessThan(200),
                modifyPlan: plan =>
                    plan
                    .AddSpiderAjaxAfterSpider()
                    .AddExcludePathsRegex(".*blog.*")
                    .DisableScanRule(10037, "Server Leaks Information via \"X-Powered-By\" HTTP Response Header Field(s)")
                    .CompletedTaskAsync()),
            browser);

    // Overriding the default setup so we can have a simpler site, simplifying the security scan for the purpose of this
    // demo. For a real app's security scan you needn't (shouldn't) do this though; always run the scan on the actual
    // app with everything set up how you run it in production.
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

                // Note how we specify an assertion too. This is because ZAP actually notices a few security issues with
                // vanilla Orchard Core. These, however, are more like artifacts of running the app locally and out of
                // the box without any real configuration. So, to make the tests pass, we need to override the default
                // assertion that would fail the test if any issue is found.

                // Don't do this at home! Fix the issues instead. This is only here to have a smoother demo.
                configuration.SecurityScanningConfiguration.AssertSecurityScanResult = (_, _) => { };

                await changeConfigurationAsync(configuration);
            });
}

// END OF TRAINING SECTION: Security scanning.
