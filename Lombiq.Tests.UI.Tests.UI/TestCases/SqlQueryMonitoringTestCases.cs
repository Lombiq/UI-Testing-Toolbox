using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.HelpfulLibraries.Samples.Controllers;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring;
using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Services;
using Lombiq.Tests.UI.TestingModule.Controllers;
using OpenQA.Selenium;
using OrchardCore.Environment.Shell;
using Shouldly;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class SqlQueryMonitoringTestCases
{
    private const string ExpectedAssertionFailureMessage = "The SQL monitoring assertion did not fail as expected.";
    private const string MissingMatchingSummaryFailureMessage =
        "The SQL monitoring assertion did not fail as expected. It should have failed due to the absence of a " +
        "matching summary for the specified request path and query.";

    public static Task SqlQueryMonitoringAdditionalQuerySourcesShouldWorkAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
        {
            await context.SqlQueryMonitoringShouldCaptureRawQueryAsync();
            await context.SqlQueryMonitoringShouldCaptureRawExecuteNonQueryAsync();
            await context.SqlQueryMonitoringShouldCaptureCustomSessionQueryAsync();
            await context.SqlQueryMonitoringShouldCaptureDirectConnectionQueryAsync();
        })
        .AddConfiguration(ConfigurationHelper.DisableHtmlValidation)
        .RunAsync();

    public static Task SqlQueryMonitoringAsyncRequestScenariosShouldWorkAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
        {
            await context.SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryAsync();

            await context.GoToHomePageAsync();
            await context.SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryWithoutPageStateWaitAsync();

            await context.GoToHomePageAsync();
            await context.SqlQueryMonitoringShouldIgnoreStaleSummariesWhenAggregatingFollowUpRequestsAsync();

            await context.GoToHomePageAsync();
            await context.SqlQueryMonitoringShouldDetectDuplicatesWithoutSpecifyingRequestPathAsync();
        })
        .RunAsync();

    public static Task SqlQueryMonitoringFailureScenariosShouldWorkAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
        {
            await context.SqlQueryMonitoringShouldSurfaceDuplicateCommandIssuesAsync();

            await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
            await context.SqlQueryMonitoringShouldSurfaceDuplicateParameterIssuesAsync();

            await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
            await context.SqlQueryMonitoringShouldSurfaceOversizedResultSetIssuesAsync();

            await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
            await context.SqlQueryMonitoringShouldSurfaceAllIssuesAsync();
        })
        .RunAsync();

    public static Task SqlQueryMonitoringRequestMatchingScenariosShouldWorkAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
        {
            await context.SqlQueryMonitoringShouldFailWhenSpecificRequestSummaryIsMissingAsync();
            await context.SqlQueryMonitoringShouldNotMatchDifferentQueryStringForSpecificRequestAsync();
        })
        .AddConfiguration(ConfigurationHelper.DisableHtmlValidation)
        .RunAsync();

    public static async Task SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryAsync(this UITestContext context)
    {
        var asyncApiPath = context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.AsyncQuery());

        await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());

        var pagePath = context.GetCurrentUri().AbsolutePath;

        context.DoWithRetriesOrFail(() =>
            string.Equals(context.GetText(By.Id("async-query-status")), "Completed", StringComparison.Ordinal));

        await context.AssertSqlQueryMonitoringForRequestAsync(
            pagePath,
            HttpMethod.Get.Method,
            summary =>
            {
                summary.Executions.ShouldNotBeEmpty(
                    "The initial page request should execute at least one SQL command.");
                return Task.CompletedTask;
            });

        await context.AssertSqlQueryMonitoringForRequestAsync(
            asyncApiPath,
            HttpMethod.Get.Method,
            summary =>
            {
                summary.Executions.ShouldNotBeEmpty(
                    "The async API request should execute at least one SQL command.");
                return Task.CompletedTask;
            });
    }

    public static async Task SqlQueryMonitoringShouldDetectDuplicatesWithoutSpecifyingRequestPathAsync(this UITestContext context)
    {
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 2;

        await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());
        var pagePath = context.GetCurrentUri().AbsolutePath;

        await AssertSqlQueryMonitoringAssertionFailsAsync(
            () => context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(),
            exception =>
            {
                exception.SqlQueryMonitoringSummary.RequestPath.ShouldContain(pagePath);
                exception.SqlQueryMonitoringSummary.RequestPath.ShouldContain("combined");
                exception.ShouldNotBeNull();
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                exception.Message.ShouldContain("Command text executed");
                exception.Message.ShouldContain("threshold: 2");
            });
    }

    public static Task SqlQueryMonitoringShouldNotCollectWhenCollectionIsDisabledAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
            {
                await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);

                await AssertInvalidOperationExceptionIsThrownAsync(
                    () => context.AssertSqlQueryMonitoringAsync(),
                    exception =>
                    {
                        exception.ShouldNotBeNull();
                        exception.Message.ShouldContain("No SQL query monitoring summary was captured.");
                    },
                    ExpectedAssertionFailureMessage);
            })
        .AddSyncConfiguration(configuration =>
            configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = false)
        .RunAsync();

    public static async Task SqlQueryMonitoringShouldSurfaceDuplicateCommandIssuesAsync(this UITestContext context)
    {
        var originalThreshold = context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 1;

        await AssertSqlQueryMonitoringAssertionFailsAsync(
            () => context.AssertSqlQueryMonitoringAsync(),
            exception =>
            {
                exception.ShouldNotBeNull();
                exception.Message.ShouldContain(
                    $"[{SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory}]");
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                exception.Message.ShouldContain("Command text executed");
                exception.Message.ShouldContain("threshold: 1");
            });

        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = originalThreshold;
    }

    public static async Task SqlQueryMonitoringShouldSurfaceDuplicateParameterIssuesAsync(this UITestContext context)
    {
        var originalThreshold = context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 1;

        await AssertSqlQueryMonitoringAssertionFailsAsync(
            () => context.AssertSqlQueryMonitoringAsync(),
            exception =>
            {
                exception.ShouldNotBeNull();
                exception.Message.ShouldContain(
                    $"[{SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory}]");
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory);
                exception.Message.ShouldContain("Command text with same parameters executed");
                exception.Message.ShouldContain("threshold: 1");
            });

        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = originalThreshold;
    }

    public static async Task SqlQueryMonitoringShouldSurfaceOversizedResultSetIssuesAsync(this UITestContext context)
    {
        var originalThreshold = context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 0;

        await AssertSqlQueryMonitoringAssertionFailsAsync(
            () => context.AssertSqlQueryMonitoringAsync(),
            exception =>
            {
                exception.ShouldNotBeNull();
                exception.Message.ShouldContain(
                    $"[{SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory}]");
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory);
                exception.Message.ShouldContain("Command result set had");
                exception.Message.ShouldContain("threshold: 0");
            });

        context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = originalThreshold;
    }

    private static async Task SqlQueryMonitoringShouldSurfaceAllIssuesAsync(this UITestContext context)
    {
        var originalDuplicateCommandThreshold =
            context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold;
        var originalDuplicateCommandWithParametersThreshold =
            context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold;
        var originalResultSetRowCountThreshold =
            context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 1;
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 1;
        context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 0;

        await AssertSqlQueryMonitoringAssertionFailsAsync(
            () => context.AssertSqlQueryMonitoringAsync(),
            exception =>
            {
                exception.ShouldNotBeNull();
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory);
                exception.Message.ShouldContain(
                    SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory);
                exception.Message.ShouldContain("Command text executed");
                exception.Message.ShouldContain("Command text with same parameters executed");
                exception.Message.ShouldContain("Command result set had");
                exception.Message.ShouldContain("threshold: 1");
                exception.Message.ShouldContain("threshold: 0");
            });

        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold =
            originalDuplicateCommandThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold =
            originalDuplicateCommandWithParametersThreshold;
        context.Configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold =
            originalResultSetRowCountThreshold;
    }

    public static Task SqlQueryMonitoringShouldWorkOnAnotherTenantAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
        {
            const string tenantName = "SqlMonitorTest";
            const string tenantUrlPrefix = "sql-monitor-test";
            const string tenantDisplayName = "Lombiq's OSOCE - SQL Monitoring Tenant";
            const string tenantAdminName = "tenantSqlMonitorAdmin";

            await context.SignInDirectlyAsync();
            await context.CreateAndSwitchToTenantAsync(
                tenantName,
                tenantUrlPrefix,
                new OrchardCoreSetupParameters
                {
                    SiteName = tenantDisplayName,
                    RecipeId = "Lombiq.OSOCE.Tests",
                    TablePrefix = tenantUrlPrefix,
                    UserName = tenantAdminName,
                });

            await context.GoToRelativeUrlAsync("/");
            var tenantPath = context.GetCurrentUri().AbsolutePath;

            await context.AssertSqlQueryMonitoringForRequestAsync(tenantPath, HttpMethod.Get.Method, summary =>
            {
                summary.TenantName.ShouldBe(tenantName);
                summary.RequestPath.ShouldStartWith($"/{tenantUrlPrefix}/");
                summary.Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
                return Task.CompletedTask;
            });

            context.SwitchCurrentTenantToDefault();
            await context.GoToRelativeUrlAsync("/");
            var defaultTenantPath = context.GetCurrentUri().AbsolutePath;

            await context.AssertSqlQueryMonitoringForRequestAsync(defaultTenantPath, HttpMethod.Get.Method, summary =>
            {
                summary.TenantName.ShouldBe(ShellSettings.DefaultShellName);
                summary.Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
                return Task.CompletedTask;
            });
        })
        .RunAsync();

    private static async Task SqlQueryMonitoringShouldFailWhenSpecificRequestSummaryIsMissingAsync(this UITestContext context)
    {
        var requestBasePath = context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
            controller => controller.RawQuery());
        var pathWithQuery = $"{requestBasePath}?missing=1";

        await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);

        await AssertInvalidOperationExceptionIsThrownAsync(
            () => context.AssertSqlQueryMonitoringForRequestAsync(pathWithQuery, HttpMethod.Get.Method),
            exception =>
            {
                exception.Message.ShouldContain(
                    "No SQL query monitoring summary was captured for",
                    customMessage: $"Exception message was: {exception.Message}");
                exception.Message.ShouldContain(
                    requestBasePath,
                    customMessage: $"Exception message was: {exception.Message}");
                exception.Message.ShouldContain(
                    "missing=1",
                    customMessage: $"Exception message was: {exception.Message}");
            },
            MissingMatchingSummaryFailureMessage);
    }

    private static async Task SqlQueryMonitoringShouldNotMatchDifferentQueryStringForSpecificRequestAsync(this UITestContext context)
    {
        var requestBasePath = context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
            controller => controller.RawQuery());
        var actualRequest = $"{requestBasePath}?request=actual";
        var expectedRequest = $"{requestBasePath}?request=expected";

        await context.GoToRelativeUrlAsync(actualRequest);

        await AssertInvalidOperationExceptionIsThrownAsync(
            () => context.AssertSqlQueryMonitoringForRequestAsync(expectedRequest, HttpMethod.Get.Method),
            exception =>
            {
                exception.Message.ShouldContain(
                    "No SQL query monitoring summary was captured for",
                    customMessage: $"Exception message was: {exception.Message}");
                exception.Message.ShouldContain(
                    requestBasePath,
                    customMessage: $"Exception message was: {exception.Message}");
            },
            MissingMatchingSummaryFailureMessage);
    }

    private static async Task SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryWithoutPageStateWaitAsync(this UITestContext context)
    {
        await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());

        await context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(summary =>
        {
            summary.Executions.Count.ShouldBeGreaterThanOrEqualTo(
                2,
                "The combined assertion should capture both page-load and async-request SQL executions.");

            summary.Executions.Count(entry =>
                    entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"))
                .ShouldBeGreaterThanOrEqualTo(2);

            return Task.CompletedTask;
        });
    }

    public static async Task SqlQueryMonitoringShouldIgnoreStaleSummariesWhenAggregatingFollowUpRequestsAsync(this UITestContext context)
    {
        await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());
        var pagePath = context.GetCurrentUri().AbsolutePath;

        await context.AssertSqlQueryMonitoringForRequestAsync(pagePath, HttpMethod.Get.Method);

        await context.GoToRelativeUrlAsync("/about");

        await context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(summary =>
        {
            summary.RequestMethod.ShouldBe(HttpMethod.Get.Method);
            summary.RequestPath.ShouldContain("/about");
            return Task.CompletedTask;
        });
    }

    public static Task LinqToDbSamplesShouldBeCapturedBySqlMonitoringAsync(UITestBase fixture) => fixture
        .ConfigureSqlMonitoring()
        .AddTest(async context =>
            {
                await context.EnableFeatureDirectlyAsync("Lombiq.HelpfulLibraries.Samples");

                var requestPath = context.GetRelativeUrlOfAction<LinqToDbSamplesController>(controller => controller.SimpleQuery());

                await context.GoToRelativeUrlAsync(requestPath);

                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.RequestPath.ShouldStartWith(
                        requestPath,
                        Case.Insensitive,
                        "The monitored summary should belong to the navigated LINQ to DB endpoint request.");

                    summary.Executions.ShouldNotBeEmpty("LINQ to DB calls should be captured by SQL query monitoring.");
                    summary.Executions.ShouldContain(entry =>
                        entry.CommandText.ContainsOrdinalIgnoreCase("FROM"));

                    return Task.CompletedTask;
                });
            })
            .AddConfiguration(ConfigurationHelper.DisableHtmlValidation)
            .RunAsync();

    public static Task SqlQueryMonitoringShouldCaptureRawQueryAsync(this UITestContext context) =>
        ExecuteSqlMonitoringScenarioAsync(
            context,
            testContext => testContext.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.RawQuery()),
            entry =>
                entry.CommandText.ContainsOrdinalIgnoreCase("SELECT") &&
                entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "The raw SQL query should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureRawExecuteNonQueryAsync(this UITestContext context) =>
        ExecuteSqlMonitoringScenarioAsync(
            context,
            requestPathMethod: testContext => testContext.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
                controller => controller.RawExecuteNonQuery()),
            entry =>
                entry.CommandText.ContainsOrdinalIgnoreCase("DELETE") &&
                entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "The raw SQL non-query command should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureCustomSessionQueryAsync(this UITestContext context) =>
        ExecuteSqlMonitoringScenarioAsync(
            context,
            requestPathMethod: testContext => testContext.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
                controller => controller.CustomSessionQuery()),
            entry => entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "Queries executed through a manually created YesSql session should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureDirectConnectionQueryAsync(this UITestContext context) =>
        ExecuteSqlMonitoringScenarioAsync(
            context,
            requestPathMethod: testContext => testContext.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
                controller => controller.DirectConnectionQuery()),
            entry =>
                entry.CommandText.ContainsOrdinalIgnoreCase("SELECT") &&
                entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "Queries executed through IDbConnectionAccessor should be captured.");

    private static Task AssertSqlQueryMonitoringAssertionFailsAsync(
        Func<Task> assertionAsync,
        Action<SqlQueryMonitoringAssertionException> assertException) =>
        AssertExceptionIsThrownAsync(assertionAsync, assertException, ExpectedAssertionFailureMessage);

    private static Task AssertInvalidOperationExceptionIsThrownAsync(
        Func<Task> assertionAsync,
        Action<InvalidOperationException> assertException,
        string failureMessage) =>
        AssertExceptionIsThrownAsync(assertionAsync, assertException, failureMessage);

    private static async Task AssertExceptionIsThrownAsync<TException>(
        Func<Task> assertionAsync,
        Action<TException> assertException,
        string failureMessage)
        where TException : Exception
    {
        var exception = await Should.ThrowAsync<TException>(assertionAsync, failureMessage);
        assertException(exception);
    }

    private static UITestBase ConfigureSqlMonitoring(this UITestBase fixture) =>
        fixture.AddSyncConfiguration(configuration =>
            configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true);

    private static async Task ExecuteSqlMonitoringScenarioAsync(
        UITestContext context,
        Func<UITestContext, string> requestPathMethod,
        Predicate<SqlQueryExecutionEntry> executionPredicate,
        string assertionMessage)
    {
        var requestPath = requestPathMethod(context);
        await context.GoToRelativeUrlAsync(requestPath);

        await context.AssertSqlQueryMonitoringAsync(summary =>
        {
            summary.RequestPath.ShouldStartWith(
                requestPath,
                Case.Insensitive,
                "The monitored summary should belong to the navigated page request.");

            summary.Executions.ShouldContain(execution => executionPredicate(execution), assertionMessage);
            return Task.CompletedTask;
        });
    }
}
