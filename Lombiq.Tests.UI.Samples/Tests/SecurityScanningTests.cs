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

// Most common alerts can be resolved by using the OrchardCoreBuilder.ConfigureSecurityDefaultsWithStaticFiles()
// extension method from Lombiq.HelpfulLibraries.OrchardCore. We use it in this in our
// https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions repo that these samples are configured for, and thus
// there are almost no alerts. It's worth enabling in in your Program and then verifying that everything still works on
// the site before really getting into security scanning. If you experience any problems related to
// Content-Security-Policy, take a look at the documentation of IContentSecurityPolicyProvider and
// ContentSecurityPolicyAttribute to adjust the permissions, because these defaults are rather strict out of the box.

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
    // similarly is an extension method as well. This can take a very long time. If you want to have a broader scan in
    // your CI test runs, use the RunAndConfigureAndAssertFullSecurityScanForContinuousIntegrationAsync extension method
    // instead. It applies some limitations to the full scan, including time limits to the active scan portion that
    // normally takes the longest.

    // If you're new to security scanning, starting with exactly this is probably a good idea. Most possibly your app
    // will fail the scan, but don't worry! You'll get a nice report about the findings in the failure dump.
    [Fact]
    public Task BasicSecurityScanShouldPass() =>
        // Note how we use a method that doesn't launch a browser. Security scanning happens fully in ZAP, and doesn't
        // use the browser launched by the UI Testing Toolbox. Not starting a browser for the test makes it a bit
        // faster. However, you can opt to launch a browser to prepare the app for security scanning if necessary.
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
            context => context.RunAndAssertBaselineSecurityScanAsync(),
            // You should configure the assertion that checks the app logs to accept some common cases that only should
            // appear during security scanning. If you launch a full scan, this is automatically configured by the
            // RunAndConfigureAndAssertFullSecurityScanForContinuousIntegrationAsync extension method.
            changeConfiguration: configuration => configuration.UseAssertAppLogsForSecurityScan());

    // Time for some custom configuration! While this scan also runs the Baseline scan, it does this with several
    // adjustments:
    // - Also runs ZAP's Ajax Spider (https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/). Usually this
    //   is only necessary for sites that are single page applications (SPA).
    // - Excludes certain URLs from the scan completely. Use this if you don't want ZAP to process those pages at all.
    // - Disables one of ZAP's passive scan rules for the whole scan.
    // - Also disables a rule but only for the /about page. Use this to disable rules more specifically instead of the
    //   whole scan.
    // - Adds a false positive rule filter for the Absence of Anti-CSRF Tokens rule, but only for the search form with
    //   the ID or class "my-search-form" which is displayed on every page. For less common filters (e.g., filters other
    //   than by URL), you can use the .ModifyZapPlan() and .AddFalsePositiveRuleFilter() extension methods to configure
    //   the action filter YAML node directly.
    // - Configures sign in with a user account. This is what the scan will start with. This doesn't matter much with
    //   the Blog recipe, because nothing on the frontend will change. You can use this to scan authenticated features
    //   too. This is necessary because ZAP uses its own spider so it doesn't share session or cookies with the browser.
    // - The assertion on the scan results is custom. Use this if you (conditionally) want to assert on the results
    //   differently from the global context.Configuration.SecurityScanningConfiguration.AssertSecurityScanResult. The
    //   default there is "no scanning alert is allowed"; we expect an alert here due to the scan visiting the
    //   intentional error page under /Lombiq.Tests.UI.Shortcuts/Error/Index.
    // - The suppressions are not actually necessary here. The BasicSecurityScanShouldPass works fine without them. They
    //   are only present to illustrate the type of adjustments you may want for your own site.
    [Fact]
    public Task SecurityScanWithCustomConfigurationShouldPass() =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
            context => context.RunAndAssertBaselineSecurityScanAsync(
                configuration => configuration
                    ////.UseAjaxSpider() // This is quite slow so just showing you here but not running it.
                    .ExcludeUrlWithRegex(".*blog.*")
                    .DisablePassiveScanRule(10020, "The response does not include either Content-Security-Policy with 'frame-ancestors' directive.")
                    .DisableScanRuleForUrlWithRegex(".*/about", 10038, "Content Security Policy (CSP) Header Not Set")
                    .ModifyZapPlan(plan => plan
                        .AddFalsePositiveRuleFilter(
                            ".*",
                            10202,
                            "Absence of Anti-CSRF Tokens",
                            "The search form doesn't alter the state of the application so anti-CSRF tokens are not needed.",
                            node =>
                            {
                                node.Children["evidence"] = ".*my-search-form.*";
                                node.Children["evidenceRegex"] = "true";
                            }))
                    .SignIn(),
                sarifLog => sarifLog.Runs[0].Results.Count.ShouldBe(0)),
            changeConfiguration: configuration => configuration.UseAssertAppLogsForSecurityScan());

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
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
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
                sarifLog => SecurityScanningConfiguration.AssertSecurityScanHasNoAlerts(context, sarifLog)),
            changeConfiguration: configuration => configuration.UseAssertAppLogsForSecurityScan());

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

                // Check out the rest of SecurityScanningConfiguration too!

                await changeConfigurationAsync(configuration);
            });
}

// END OF TRAINING SECTION: Security scanning.
// NEXT STATION: Head over to Tests/RemoteTests.cs.
