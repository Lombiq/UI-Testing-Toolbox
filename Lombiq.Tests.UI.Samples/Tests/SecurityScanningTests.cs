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
using YamlDotNet.RepresentationModel;

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
    [Fact]
    public Task BasicSecurityScanShouldPass() =>
        ExecuteTestAfterSetupAsync(context => context.RunAndAssertBaselineSecurityScanAsync());

    // Time for some custom configuration! While this scan also runs the Baseline scan, it does this with several
    // adjustments:
    // - Also runs ZAP's Ajax Spider (https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/). This is
    //   usually not just unnecessary for a website that's not an SPA, but also slows the scan down by a lot. However,
    //   if you have an SPA, you need to use it.
    // - Excludes certain URLs from the scan completely. Use this if you don't want ZAP to process certain URLs at all.
    // - Disables the "Server Leaks Information via "X-Powered-By" HTTP Response Header Field(s)" alert of ZAP's passive
    //   scan for the whole scan. This is because by default, Orchard Core sends an "X-Powered-By: OrchardCore" header.
    //   If you want airtight security, you might want to turn this off, but for the sake of example we just ignore the
    //   alert here.
    // - Also disables the "Content Security Policy (CSP) Header Not Set" rule but only for the /about page. Use this to
    //   disable rules more specifically instead of the whole scan.
    // - Configures sign in with a user account. This is what the scan will start with. With the Blog recipe it doesn't
    //   matter too much, since nothing on the frontend will change, but you can use this to scan authenticated features
    //   too. Note that since ZAP uses its own spider, not the browser accessed by the test, user sessions are not
    //   shared, so such an explicit sign in is necessary.
    // - The assertion on the scan results is custom. Use this if you (conditionally) want to assert on the results
    //   differently from the global context.Configuration.SecurityScanningConfiguration.AssertSecurityScanResult. The
    //   default there is "no scanning alert is allowed"; we expect some alerts here.
    [Fact]
    public Task SecurityScanWithCustomConfigurationShouldPass() =>
        ExecuteTestAfterSetupAsync(
            context => context.RunAndAssertBaselineSecurityScanAsync(
                configuration => configuration
                    ////.UseAjaxSpider() // This is quite slow so just showing you here but not running it.
                    .ExcludeUrlWithRegex(".*blog.*")
                    .DisablePassiveScanRule(10020, "The response does not include either Content-Security-Policy with 'frame-ancestors' directive.")
                    .DisableScanRuleForUrlWithRegex(".*/about", 10038, "Content Security Policy (CSP) Header Not Set")
                    .SignIn(),
                sarifLog => sarifLog.Runs[0].Results.Count.ShouldBeLessThan(34)));

    // Let's get low-level into ZAP's configuration now. While the .NET configuration API of the Lombiq UI Testing
    // Toolbox covers the most important ways to configure ZAP, sometimes you need more. For this, you have complete
    // control over ZAP's configuration via its Automation Framework (see
    // https://www.zaproxy.org/docs/automate/automation-framework/ and https://www.youtube.com/watch?v=PnCbIAnauD8 for
    // an introduction), what all the packaged scans and .NET configuration uses under the hood too. This way, if you
    // know what you want to do in ZAP, you can just directly run in as a UI test!

    // We run a completely custom Automation Framework plan here. It's almost the same as the plan used by the Baseline
    // scan, but has some rules disabled by default, so we can assert on no alerts. Note that it has the Content build
    // action to copy it to the build output folder.

    // You can also create and configure such plans from the ZAP desktop app, following the guides linked above. The
    // plan doesn't need anything special, apart from having at least one context defined, as well as having a
    // "sarif-json" report job so assertions can work with it. If something is missing in it, you'll get exceptions
    // telling you what the problem is anyway.

    // Then, you can see an example of modifying the ZAP plan from code. You can also do this with the built-in plans to
    // customize them if something you need is not surfaced as configuration.
    [Fact]
    public Task SecurityScanWithCustomAutomationFrameworkPlanShouldPass() =>
        ExecuteTestAfterSetupAsync(
            context => context.RunAndAssertSecurityScanAsync(
                "Tests/CustomZapAutomationFrameworkPlan.yml",
                configuration => configuration
                    .ModifyZapPlan(plan =>
                    {
                        // "plan" here is a representation of the YAML document containing the plan. It's a low-level
                        // representation, but you can do anything with it.

                        // We'll change a parameter for ZAP's spider. This of course could be done right in our custom
                        // plan, but we wanted to demo this too; furthermore, from code, you can change the plan even
                        // based on the context dynamically, so it's more flexible than trying to configure everything
                        // in the plan's YAML file.

                        var spiderJob = plan.GetSpiderJob();
                        var spiderParameters = (YamlMappingNode)spiderJob["parameters"];
                        // The default maxDepth is 5. 8 will let the spider run for a bit more, potentially discovering
                        // more pages to be scanned.
                        spiderParameters.Add("maxDepth", "8");
                    }),
                sarifLog => SecurityScanningConfiguration.AssertSecurityScanHasNoAlerts(context, sarifLog)));

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
                // Check out the rest of SecurityScanningConfiguration too!

                await changeConfigurationAsync(configuration);
            });
}

// END OF TRAINING SECTION: Security scanning.
