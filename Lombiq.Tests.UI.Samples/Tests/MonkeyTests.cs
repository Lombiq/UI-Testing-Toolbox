using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.MonkeyTesting.UrlFilters;
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
                async context =>
                {
                    // Note how we define the starting point of the test as the homepage.
                    await context.GoToHomePageAsync();
                    // The specified random see gives you the option to reproduce the random interactions. Otherwise
                    // it would be calculated from MonkeyTestingOptions.BaseRandomSeed.
                    await context.TestCurrentPageAsMonkeyAsync(CreateMonkeyTestingOptions(), 12345);
                },
                browser);

        // Recursive testing will just continue testing following the configured rules until it runs out of time or new
        // pages.
        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAnonymousUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    await context.GoToHomePageAsync();
                    await context.TestCurrentPageAsMonkeyRecursivelyAsync(CreateMonkeyTestingOptions());

                    // The shortcut context.TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(_monkeyTestingOptions)
                    // does the same thing but we wanted to demonstrate the contrast with
                    // TestCurrentPageAsMonkeyShouldWorkWithConfiguredRandomSeed().
                },
                browser);

        // Let's test with an authenticated user too.
        [Theory, Chrome]
        public Task TestAdminPagesAsMonkeyRecursivelyShouldWorkWithAdminUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                    // Monkey tests needn't all start from the homepage. This one starts from the Orchard admin
                    // dashboard.
                    context.TestAdminAsMonkeyRecursivelyAsync(CreateMonkeyTestingOptions()),
                browser);

        // Let's just test the background tasks management admin area.
        [Theory, Chrome]
        public Task TestAdminBackgroundTasksAsMonkeyRecursivelyShouldWorkWithAdminUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    var monkeyTestingOptions = CreateMonkeyTestingOptions();

                    // You can fence monkey testing with URL filters: Monkey testing will only be executed if the
                    // current URL matches. This way, you can restrict monkey testing to just sections of the site. You
                    // can also use such fencing to have multiple monkey testing methods in multiple test classes, thus
                    // running them in parallel.
                    monkeyTestingOptions.UrlFilters.Add(new StartsWithMonkeyTestingUrlFilter("/Admin/BackgroundTasks"));
                    // You could also configure the same thing with regex:
                    ////_monkeyTestingOptions.UrlFilters.Add(new MatchesRegexMonkeyTestingUrlFilter(@"\/Admin\/BackgroundTasks"));

                    await context.SignInDirectlyAndGoToRelativeUrlAsync("/Admin/BackgroundTasks");
                    await context.TestCurrentPageAsMonkeyRecursivelyAsync(monkeyTestingOptions);
                },
                browser);

        // Monkey testing has its own configuration too. Check out the docs of the options too.
        private static MonkeyTestingOptions CreateMonkeyTestingOptions() =>
            new()
            {
                PageTestTime = TimeSpan.FromSeconds(10),
            };
    }
}

// END OF TRAINING SECTION: Monkey tests.
