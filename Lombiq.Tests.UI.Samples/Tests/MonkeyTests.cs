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
    // click, scrolling, form filling, etc. Such random actions can uncover bugs that are otherwise difficult to find.
    // Use such tests plug holes in your test suite which are not covered by explicit tests.
    public class MonkeyTests : UITestBase
    {
        // Monkey testing has its own configuration too. Check out the docs of the options too.
        private readonly MonkeyTestingOptions _monkeyTestingOptions = new()
        {
            PageTestTime = TimeSpan.FromSeconds(10),
            BaseRandomSeed = 1234,
        };

        public MonkeyTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // The basic idea is that you unleash monkey testing on specific pages or sections of the site, like a contact
        // form or the content management UI.
        // First, we test a single page.
        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyShouldWorkWithConfiguredRandomSeed(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    // Note how we define the starting point of the test as the homepage.
                    context.GoToHomePage();
                    // The specified random see gives you the option to reproduce the random interactions. Otherwise
                    // it would be calculated from MonkeyTestingOptions.BaseRandomSeed.
                    context.TestCurrentPageAsMonkey(_monkeyTestingOptions, 12345);
                },
                browser);

        // Recursive testing will just continue testing following the configured rules until it runs out time or new
        // pages.
        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAnonymousUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToHomePage();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);

        // Let's test with an authenticated user too.
        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAdminUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    // Monkey tests needn't all start from the homepage. This one starts from the Orchard admin
                    // dashboard.
                    context.SignInDirectlyAndGoToDashboard();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);
    }
}

// END OF TRAINING SECTION: Monkey tests.
