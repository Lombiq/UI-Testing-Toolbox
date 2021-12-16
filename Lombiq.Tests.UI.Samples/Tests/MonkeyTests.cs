using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // It's possible to execute monkey tests that walk through site pages and do random interactions with pages, like
    // click, scrolling, form filling, etc. Such random actions can uncover bugs that are difficult to find.
    public class MonkeyTests : UITestBase
    {
        private readonly MonkeyTestingOptions _monkeyTestingOptions = new()
        {
            PageTestTime = TimeSpan.FromSeconds(10),
            BaseRandomSeed = 1234,
            RunAccessibilityCheckingAssertion = false,
            RunHtmlValidationAssertion = false,
        };

        public MonkeyTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyShouldWorkWithConfiguredRandomSeed(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToHomePage();
                    context.TestCurrentPageAsMonkey(_monkeyTestingOptions, 12345);
                },
                browser);

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAnonymousUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToHomePage();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAdminUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.SignInDirectlyAndGoToDashboard();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);
    }
}

// END OF TRAINING SECTION: Monkey tests.
