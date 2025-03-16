using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.MonkeyTesting.UrlFilters;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// It's possible to execute monkey tests that walk through site pages and do random interactions with pages, like click,
// scrolling, form filling, etc. Such random actions can uncover bugs that are otherwise difficult to find. Use such
// tests plug holes in your test suite which are not covered by explicit tests.
public class MonkeyTests : UITestBase
{
    public MonkeyTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // The basic idea is that you unleash monkey testing on specific pages or sections of the site, like a contact form
    // or the content management UI. First, we test a single page.
    [Fact]
    public Task TestCurrentPageAsMonkeyShouldWorkWithConfiguredRandomSeed() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // Note how we define the starting point of the test as the homepage.
                await context.GoToHomePageAsync();
                // The specified random see gives you the option to reproduce the random interactions. Otherwise it
                // would be calculated from MonkeyTestingOptions.BaseRandomSeed.
                await context.TestCurrentPageAsMonkeyAsync(CreateMonkeyTestingOptions(), 12345);
            });

    // Recursive testing will just continue testing following the configured rules until it runs out of time or new
    // pages.
    [Fact]
    public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAnonymousUser() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.GoToHomePageAsync();
                await context.TestCurrentPageAsMonkeyRecursivelyAsync(CreateMonkeyTestingOptions());

                // The shortcut context.TestFrontendAuthenticatedAsMonkeyRecursivelyAsync(_monkeyTestingOptions) does
                // the same thing but we wanted to demonstrate the contrast with
                // TestCurrentPageAsMonkeyShouldWorkWithConfiguredRandomSeed().
            });

    // Let's test with an authenticated user too.
    [Fact]
    public Task TestAdminPagesAsMonkeyRecursivelyShouldWorkWithAdminUser() =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                // Monkey tests needn't all start from the homepage. This one starts from the Orchard admin dashboard.

                var monkeyTestingOptions = CreateMonkeyTestingOptions();

                // So we don't take too much time testing the whole Orchard admin, this sample restricts requests to
                // "/Admin". But this is just this sample, you can unleash monkeys on the whole admin too!
                monkeyTestingOptions.UrlFilters.Add(new MatchesRegexMonkeyTestingUrlFilter("/Admin$"));

                // You can fence monkey testing with URL filters: Monkey testing will only be executed if the current
                // URL matches. This way, you can restrict monkey testing to just sections of the site. You can also use
                // such fencing to have multiple monkey testing methods in multiple test classes, thus running them in
                // parallel. Another option apart from regex is e.g. StartsWithMonkeyTestingUrlFilter, with which you
                // can do things like this:
                ////monkeyTestingOptions.UrlFilters.Add(new StartsWithMonkeyTestingUrlFilter("/Admin/BackgroundTasks"));
                // Explore all the options in the Lombiq.Tests.UI.MonkeyTesting.UrlFilters namespace.

                // With this method, you can test the whole (barring restrictions like above) admin recursively. But you
                // can use TestCurrentPageAsMonkeyRecursivelyAsync() on the admin too.
                return context.TestAdminAsMonkeyRecursivelyAsync(monkeyTestingOptions);
            },
            // Requests to /api/graphql without further parameters will fail with HTTP 400, but that's OK, since some
            // parameters are required.
            configuration => configuration.ResponseLogFilter = e => e.IsNonSuccessResponseAndNotExpectedStatusResponse("/api/graphql", 400));

    // Monkey testing has its own configuration too. Check out the docs of the options too.
    private static MonkeyTestingOptions CreateMonkeyTestingOptions() =>
        new()
        {
            PageTestTime = TimeSpan.FromSeconds(5),
        };
}

// END OF TRAINING SECTION: Monkey tests.
// NEXT STATION: Head over to Tests/DatabaseSnapshotTests.cs.
