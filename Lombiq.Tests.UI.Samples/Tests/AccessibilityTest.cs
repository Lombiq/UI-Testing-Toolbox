using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // Here we'll see how to check some web accessibility rules. Keeping our app accessible helps people with
    // disabilities consume the content of our website more easily. Do note though that only checking rules that can be
    // automatically checked is not enough for full compliance.
    public class AccessibilityTest : UITestBase
    {
        public AccessibilityTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task FrontendPagesShoudlBeAccessbile(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                    // This is just a simple test that visits two pages: The homepage, where the test will start by
                    // default, and another one.
                    context.GoToRelativeUrlAsync("/categories/travel"),
                browser,
                configuration =>
                {
                    // We adjust the configuration just for this test but you could do the same globally in UITestBase.

                    // With this config, accessibility rules will be checked for each page automatically.
                    configuration.AccessibilityCheckingConfiguration.RunAccessibilityCheckingAssertionOnAllPageChanges = true;

                    // We'll check for the WCAG 2.1 AA level. This is the middle level of the latest accessibility
                    // guidelines.
                    // The frontend set up with the Blog recipe actually has a couple of issues. For the sake of this
                    // sample we won't try to fix them but rather disable the corresponding rules.
                    configuration.AccessibilityCheckingConfiguration.AxeBuilderConfigurator += axeBuilder =>
                        AccessibilityCheckingConfiguration.ConfigureWcag21aa(axeBuilder)
                            .DisableRules("color-contrast", "landmark-one-main", "link-name", "region");
                });
    }
}

// END OF TRAINING SECTION: Accessibility tests.
// NEXT STATION: Head over to Tests/SqlServerTests.cs.
