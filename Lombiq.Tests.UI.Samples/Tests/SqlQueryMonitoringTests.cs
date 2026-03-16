using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;
using Lombiq.Tests.UI.TestingModule.Controllers;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using static Lombiq.Tests.UI.SqlQueryMonitoring.Services.SqlQueryMonitoringConfiguration;

namespace Lombiq.Tests.UI.Samples.Tests;

// SQL query monitoring can catch performance issues like duplicate queries and too large result sets right from a UI
// test. This sample shows the basic ways you can use it:
// - Enabling SQL monitoring for the app under test.
// - Asserting against the latest monitored request.
// - Asserting against a specific request path (including the query string).
// - Combining the page request with follow-up async requests (like the ones an SPA makes) into one assertion.
// - Running SQL monitoring automatically on selected page changes.
// - Customizing thresholds per page, first with a BeforeNavigation handler, then with URL-pattern rules.
// - Filtering out known noisy SQL commands that you explicitly don't care about.
public class SqlQueryMonitoringTests : UITestBase
{
    public SqlQueryMonitoringTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SqlQueryMonitoringExplicitAssertions() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // SQL monitoring is already enabled for this test, so even the request that opened the home page has a
                // captured summary. The simplest assertion checks the latest request for the current page.
                await context.AssertSqlQueryMonitoringAsync();

                // If you want to assert against a specific request instead, you can do that too.
                await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
                await context.AssertSqlQueryMonitoringForRequestAsync("/", HttpMethod.Get.Method);

                // If you want to include follow-up requests in the assertion, you can use this one. In this case it
                // won't find any follow-up requests, but it will still assert against the main page request and its SQL
                // summary.
                await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
                await context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync();
            },
            changeConfiguration: configuration =>
                // SQL monitoring is off by default, so you need to turn it on for the test.
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true);

    [Fact]
    public Task SqlQueryMonitoringAutomaticAssertsOnPageChangeWithFilter()
    {
        // This list is only here to show that automatic page-change assertions can be used together with explicit
        // assertions in one test.
        var automaticallyAssertedSummaries = new List<SqlQueryMonitoringSummary>();

        return ExecuteTestAfterSetupAsync(
            async context =>
            {
                // The automatic page-change assertion below only runs on /about. This shows that mode without getting
                // in the way of the later explicit assertions.
                await context.GoToRelativeUrlAsync("/about");

                // We don't know if the GoToRelativeUrlAsync had to retry the navigation while still triggering the page
                // load on the server side, so we might have more than one summary for the page changes. But only the
                // ones for /about should be there.
                automaticallyAssertedSummaries.Count(summary => !summary.RequestPath.Contains("/about"))
                    .ShouldBe(0, "Only the page changes that match the configured rule should be in the summaries");
                automaticallyAssertedSummaries[0].RequestMethod.ShouldBe(HttpMethod.Get.Method);
                automaticallyAssertedSummaries[0].RequestPath.ShouldContain("/about");
                automaticallyAssertedSummaries[0].Executions.ShouldNotBeEmpty(
                    "Automatic page-change assertions should receive the monitored SQL summary too.");

                // If you want full control over thresholds for the next navigation, you can hook into
                // BeforeNavigation. Here we navigate to a page with a query string, then assert against that exact
                // request.
                const string requestPathWithQuery = "/categories/travel?sqlMonitoringSample=before-navigation";
                await context.GoToRelativeUrlAsync(requestPathWithQuery);

                await context.AssertSqlQueryMonitoringForRequestAsync(
                    requestPathWithQuery,
                    HttpMethod.Get.Method,
                    summary =>
                    {
                        summary.RequestPath.ShouldStartWith(
                            requestPathWithQuery,
                            Case.Insensitive,
                            "Request-specific assertions should match the exact path and query string.");
                        summary.Executions.ShouldNotBeEmpty(
                            "The request-specific assertion should locate the SQL summary for this exact request.");
                        return Task.CompletedTask;
                    });
            },
            changeConfiguration: configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;

                // This enables automatic page-change assertions, but only on /about. That way the later explicit
                // assertions can still use their own summaries.
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
                configuration.SqlQueryMonitoringConfiguration.SqlQueryMonitoringAndAssertionOnPageChangeRule =
                    context => context.GetCurrentUri().AbsolutePath.EqualsOrdinalIgnoreCase("/about");

                // If you want to observe or custom-assert some pages automatically, you can wire the page-change
                // assertion up like this.
                configuration.SetUpSqlQueryMonitoringAssertionOnPageChange(summary =>
                {
                    automaticallyAssertedSummaries.Add(summary);
                    summary.Executions.ShouldNotBeEmpty(
                        "Automatically asserted summaries should also expose the monitored SQL executions.");
                    return Task.CompletedTask;
                });

                // You can filter out known noisy queries too.
                configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
                    SqlQueryMonitoringHelpers.BuildIgnoreCommandTextPatternFilter(
                        @"FROM\s+\[Document\].*RolesDocument");

                // This is the more manual way to set page-specific thresholds. It lets you use any logic you want based
                // on the target URI. You could also change these thresholds just once, for all pages, without using
                // BeforeNavigation.
                configuration.Events.BeforeNavigation += (_, targetUri) =>
                {
                    var thresholds = configuration.SqlQueryMonitoringConfiguration;

                    if (targetUri.AbsolutePath.ContainsOrdinalIgnoreCase("/categories"))
                    {
                        thresholds.DuplicateCommandThreshold = 20;
                        thresholds.DuplicateCommandWithParametersThreshold = 10;
                        thresholds.ResultSetRowCountThreshold = 100;
                    }
                    else
                    {
                        thresholds.DuplicateCommandThreshold = 30;
                        thresholds.DuplicateCommandWithParametersThreshold = 15;
                        thresholds.ResultSetRowCountThreshold = 200;
                    }

                    return Task.CompletedTask;
                };
            });
    }

    [Fact]
    public Task SqlQueryMonitoringAutomaticAssertsOnPageChangeWithRegexThresholdSettings() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.RequestPath.ShouldContain("/categories/travel");
                    summary.Executions.ShouldNotBeEmpty(
                        "The current-page assertion still works after changing thresholds with the regex helper.");
                    return Task.CompletedTask;
                });
            },
            changeConfiguration: configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;

                // If your threshold rules are only based on the URL, then the regex helper is a bit more compact for
                // multiple URL patterns. The default threshold can also be set with it, so the not matched URLs will
                // use that.
                configuration.ConfigureSqlQueryMonitoringThresholdsForPages(
                    new SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 30,
                        DuplicateCommandWithParametersThreshold: 15,
                        ResultSetRowCountThreshold: 200),
                    (Pattern: @"^/categories/.*", Thresholds: new SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 20,
                        DuplicateCommandWithParametersThreshold: 10,
                        ResultSetRowCountThreshold: 100)),
                    (Pattern: @"^/about$", Thresholds: new SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 25,
                        DuplicateCommandWithParametersThreshold: 12,
                        ResultSetRowCountThreshold: 150)));
            });

    [Fact]
    public Task SqlQueryMonitoringPageLoadFollowingAsyncRequests() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // This scenario page starts an async request after the initial page load. The async request has its own
                // SQL summary, but it can be asserted together with the page request too.
                var asyncApiPath =
                    context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.AsyncQuery());

                // Go to the page that starts the async API call, then assert against both the page request and the
                // async API request.
                await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());
                var pagePath = context.GetCurrentUri().AbsolutePath;

                // The async API call finishes in the background, so wait until the page shows that it's done.
                context.DoWithRetriesOrFail(() => context.GetText(By.Id("async-query-status")).EqualsOrdinalIgnoreCase("Completed"));

                await context.AssertSqlQueryMonitoringForRequestAsync(
                    pagePath,
                    HttpMethod.Get.Method,
                    summary =>
                    {
                        summary.RequestPath.ShouldBe(pagePath);
                        summary.Executions.ShouldNotBeEmpty(
                            "The initial page request should have its own SQL monitoring summary.");
                        return Task.CompletedTask;
                    });

                await context.AssertSqlQueryMonitoringForRequestAsync(
                    asyncApiPath,
                    HttpMethod.Get.Method,
                    summary =>
                    {
                        summary.RequestPath.ShouldBe(asyncApiPath);
                        summary.Executions.ShouldNotBeEmpty(
                            "The async API request should also have its own SQL monitoring summary.");
                        return Task.CompletedTask;
                    });

                // Or if you don't care which follow-up request caused the problem, only that the page and its async
                // requests together did, then the follow-up-inclusive assertion combines those summaries.
                await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());

                await context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(summary =>
                {
                    summary.RequestMethod.ShouldBe("MULTI");
                    summary.RequestPath.ShouldContain("combined");
                    summary.Executions.Count.ShouldBeGreaterThanOrEqualTo(
                        2,
                        "The combined assertion should contain executions from both the page request and the async API call.");
                    return Task.CompletedTask;
                });
            },
            configuration => configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true);
}

// END OF TRAINING SECTION: SQL query monitoring tests.
