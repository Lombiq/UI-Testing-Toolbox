using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;
using Lombiq.Tests.UI.Tests.UI.Controllers;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Collections.Generic;
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
// - Combining the page request with follow-up async requests into one assertion.
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
    public Task SqlQueryMonitoringShouldWork()
    {
        // This list is only here to show that automatic page-change assertions can be used together with explicit
        // assertions in one test.
        var automaticallyAssertedSummaries = new List<SqlQueryMonitoringSummary>();
        NavigationEventHandler perPageThresholdsBeforeNavigation = null;

        return ExecuteTestAfterSetupAsync(
            async context =>
            {
                // SQL monitoring is already enabled for this test, so even the request that opened the home page has a
                // captured summary. The simplest assertion always checks the latest request for the current page.
                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.RequestMethod.ShouldBe(HttpMethod.Get.Method);
                    summary.RequestPath.ShouldStartWith("/");
                    summary.Executions.ShouldNotBeEmpty(
                        "The home page should execute at least one SQL command when monitoring is enabled.");
                    return Task.CompletedTask;
                });

                // The automatic page-change assertion below only runs on /about. This shows that mode without getting
                // in the way of the later explicit assertions.
                await context.GoToRelativeUrlAsync("/about");

                automaticallyAssertedSummaries.Count.ShouldBe(1);
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

                // We only wanted to show the event-based threshold approach once. Remove it so the next part can show
                // the regex helper separately.
                context.Configuration.Events.BeforeNavigation -= perPageThresholdsBeforeNavigation;

                // If your threshold rules are only based on the URL, then the regex helper is a bit more compact for
                // multiple URL patterns. The default threshold can also be set with it, so the not matched URLs will
                // use that.
                context.Configuration.ConfigureSqlQueryMonitoringThresholdsForPages(
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

                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.RequestPath.ShouldContain("/categories/travel");
                    summary.Executions.ShouldNotBeEmpty(
                        "The current-page assertion still works after changing thresholds with the regex helper.");
                    return Task.CompletedTask;
                });

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

                // Sometimes you don't care which follow-up request caused the problem, only that the page and its async
                // requests work together did. The follow-up-inclusive assertion combines those summaries.
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
            configuration =>
            {
                // SQL monitoring is off by default, so you need to turn it on for the test.
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;

                // This enables automatic page-change assertions, but only on /about. That way the later explicit
                // assertions can still use their own summaries.
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

                // This is the more manual way to set page-specific thresholds. It lets you use any logic you want
                // based on the target URI. We'll remove it later so the sample can also show the regex helper.
                perPageThresholdsBeforeNavigation = (_, targetUri) =>
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

                configuration.Events.BeforeNavigation += perPageThresholdsBeforeNavigation;
            });
    }
}
