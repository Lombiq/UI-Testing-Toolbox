using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Lombiq.Tests.UI.SqlQueryMonitoring;
using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;
using Lombiq.Tests.UI.SqlQueryMonitoring.Services;
using OpenQA.Selenium;
using OrchardCore.Environment.Shell;
using Shouldly;
using System.Configuration;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class SqlQueryMonitoringTestCases
{
    private const string ExpectedAssertionFailureMessage = "The SQL monitoring assertion did not fail as expected.";
    private const string MissingMatchingSummaryFailureMessage =
        "The SQL monitoring assertion did not fail as expected. It should have failed due to the absence of a " +
        "matching summary for the specified request path and query.";

    public static Task SqlQueryMonitoringShouldCatchDuplicatesAndLargeResultsAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.AssertSqlQueryMonitoringAsync();

                await context.GoToRelativeUrlAsync("/categories/travel");

                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
                    return Task.CompletedTask;
                });
            },
            browser);

    public static Task SqlQueryMonitoringShouldAllowPerPageThresholdsAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.GoToRelativeUrlAsync("/about");
            },
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
                configuration.Events.BeforeNavigation += (_, targetUri) =>
                {
                    var thresholds = configuration.SqlQueryMonitoringConfiguration;

                    if (targetUri.AbsolutePath.Contains("/categories"))
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

                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldAllowRegexBasedPerPageThresholdsAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.GoToRelativeUrlAsync("/about");
                await context.GoToRelativeUrlAsync("/");
            },
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
                configuration.ConfigureSqlQueryMonitoringThresholdsForPages(
                    new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 30,
                        DuplicateCommandWithParametersThreshold: 15,
                        ResultSetRowCountThreshold: 200),
                    (Pattern: @"^/categories/.*", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 20,
                        DuplicateCommandWithParametersThreshold: 10,
                        ResultSetRowCountThreshold: 100)),
                    (Pattern: @"^/about$", Thresholds: new SqlQueryMonitoringConfiguration.SqlQueryMonitoringThresholds(
                        DuplicateCommandThreshold: 25,
                        DuplicateCommandWithParametersThreshold: 12,
                        ResultSetRowCountThreshold: 150)));
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldRespectPageChangeRuleAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default)
    {
        var summaries = new List<SqlQueryMonitoringSummary>();

        return ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.GoToRelativeUrlAsync("/categories/travel");
                await context.GoToRelativeUrlAsync("/about");

                summaries.Count.ShouldBe(1);
                summaries[0].RequestPath.ShouldContain("/categories");
                summaries[0].Executions.ShouldNotBeEmpty("SQL query monitoring should capture at least one command.");
            },
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.RunSqlQueryMonitoringAssertionOnAllPageChanges = true;
                configuration.SqlQueryMonitoringConfiguration.SqlQueryMonitoringAndAssertionOnPageChangeRule =
                    context => context.GetCurrentUri().AbsolutePath.Contains("/categories");
                configuration.SqlQueryMonitoringConfiguration.AssertSqlQueryMonitoringSummaryAsync = summary =>
                {
                    summaries.Add(summary);
                    return Task.CompletedTask;
                };

                return Task.CompletedTask;
            });
    }

    public static Task SqlQueryMonitoringShouldAllowIgnoringKnownQueriesAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            context => context.AssertSqlQueryMonitoringAsync(),
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 5;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 3;
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 5;

                configuration.SqlQueryMonitoringConfiguration.ExecutionFilter =
                    SqlQueryMonitoringConfiguration.BuildIgnoreCommandTextPatternFilter(
                        @"FROM\s+\[Document\].*\[Type\]\s*=\s*@Type",
                        @"FROM\s+\[Document\].*ContentDefinitionRecord",
                        @"FROM\s+\[Document\].*RolesDocument",
                        @"FROM\s+\[Document\].*PlacementsDocument",
                        @"FROM\s+\[Document\].*LayersDocument",
                        @"FROM\s+\[Document\].*TemplatesDocument",
                        @"FROM\s+\[ContentItemIndex\]",
                        @"FROM\s+\[AutoroutePartIndex\]");

                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser);

    public static Task SqlQueryMonitoringShouldDetectDuplicatesWithoutSpecifyingRequestPathAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.GoToAsync<SqlQueryMonitoringScenarioController>(controller => controller.Index());
                var pagePath = context.GetCurrentUri().AbsolutePath;

                await AssertSqlQueryMonitoringAssertionFailsAsync(
                    () => context.AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(),
                    exception =>
                    {
                        exception.SqlQueryMonitoringSummary.RequestPath.ShouldContain(pagePath);
                        exception.SqlQueryMonitoringSummary.RequestPath.ShouldContain("combined");
                        exception.InnerException.ShouldNotBeNull();
                        exception.InnerException.Message.ShouldContain(
                            SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                        exception.InnerException.Message.ShouldContain("Command text executed");
                        exception.InnerException.Message.ShouldContain("threshold: 2");
                    });
            },
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 2;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldNotCollectWhenCollectionIsDisabledAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = false;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldSurfaceDuplicateCommandIssuesAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            context => AssertSqlQueryMonitoringAssertionFailsAsync(
                () => context.AssertSqlQueryMonitoringAsync(),
                exception =>
                {
                    exception.InnerException.ShouldNotBeNull();
                    exception.InnerException.Message.ShouldContain(
                        $"[{SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory}]");
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                    exception.InnerException.Message.ShouldContain("Command text executed");
                    exception.InnerException.Message.ShouldContain("threshold: 1");
                }),
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 1;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldSurfaceDuplicateParameterIssuesAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            context => AssertSqlQueryMonitoringAssertionFailsAsync(
                () => context.AssertSqlQueryMonitoringAsync(),
                exception =>
                {
                    exception.InnerException.ShouldNotBeNull();
                    exception.InnerException.Message.ShouldContain(
                        $"[{SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory}]");
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory);
                    exception.InnerException.Message.ShouldContain("Command text with same parameters executed");
                    exception.InnerException.Message.ShouldContain("threshold: 1");
                }),
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 1;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldSurfaceOversizedResultSetIssuesAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            context => AssertSqlQueryMonitoringAssertionFailsAsync(
                () => context.AssertSqlQueryMonitoringAsync(),
                exception =>
                {
                    exception.InnerException.ShouldNotBeNull();
                    exception.InnerException.Message.ShouldContain(
                        $"[{SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory}]");
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory);
                    exception.InnerException.Message.ShouldContain("Command result set had");
                    exception.InnerException.Message.ShouldContain("threshold: 0");
                }),
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 0;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldSurfaceAllIssuesAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            context => AssertSqlQueryMonitoringAssertionFailsAsync(
                () => context.AssertSqlQueryMonitoringAsync(),
                exception =>
                {
                    exception.InnerException.ShouldNotBeNull();
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.DuplicateCommandFailureCategory);
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersFailureCategory);
                    exception.InnerException.Message.ShouldContain(
                        SqlQueryMonitoringConfiguration.ResultSetRowCountFailureCategory);
                    exception.InnerException.Message.ShouldContain("Command text executed");
                    exception.InnerException.Message.ShouldContain("Command text with same parameters executed");
                    exception.InnerException.Message.ShouldContain("Command result set had");
                    exception.InnerException.Message.ShouldContain("threshold: 1");
                    exception.InnerException.Message.ShouldContain("threshold: 0");
                }),
            browser,
            configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandThreshold = 1;
                configuration.SqlQueryMonitoringConfiguration.DuplicateCommandWithParametersThreshold = 1;
                configuration.SqlQueryMonitoringConfiguration.ResultSetRowCountThreshold = 0;
                return Task.CompletedTask;
            });

    public static Task SqlQueryMonitoringShouldWorkOnAnotherTenantAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser);

    public static Task SqlQueryMonitoringShouldCaptureRequestPathAndQueryForNavigatedPageAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                const string requestPath = "/categories/travel?sqlMonitoringRequestCheck=1";

                await context.GoToRelativeUrlAsync(requestPath);

                await context.AssertSqlQueryMonitoringAsync(summary =>
                {
                    summary.Executions.ShouldNotBeEmpty("Page requests should be captured.");
                    summary.RequestPath.ShouldStartWith(
                        requestPath,
                        Case.Insensitive,
                        "The request path and query should be captured in the summary and match the navigated path.");
                    return Task.CompletedTask;
                });
            },
            browser);

    public static Task SqlQueryMonitoringShouldFailWhenSpecificRequestSummaryIsMissingAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser);

    public static Task SqlQueryMonitoringShouldNotMatchDifferentQueryStringForSpecificRequestAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    public static Task SqlQueryMonitoringShouldCapturePageLoadAndAsyncApiQueryWithoutPageStateWaitAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser);

    public static Task SqlQueryMonitoringShouldIgnoreStaleSummariesWhenAggregatingFollowUpRequestsAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser);

    public static Task SqlQueryMonitoringShouldRetainRecentSqlSummariesAmidNoisyRequestsAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                var sqlRequestPath =
                    context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.RawQuery());
                var noSqlRequestPath =
                    context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.NoSql());
                const int noSqlRequestsToGenerate = 55; // Exceeds SqlQueryMonitoringStore.MaxEntries.

                await context.GoToRelativeUrlAsync(sqlRequestPath);

                for (var requestIndex = 0; requestIndex < noSqlRequestsToGenerate; requestIndex++)
                {
                    await context.GoToRelativeUrlAsync(
                        $"{noSqlRequestPath}?request={requestIndex.ToTechnicalString()}",
                        onlyIfNotAlreadyThere: false);
                }

                await context.AssertSqlQueryMonitoringForRequestAsync(
                    sqlRequestPath,
                    HttpMethod.Get.Method,
                    summary =>
                    {
                        summary.Executions.ShouldNotBeEmpty(
                            "SQL request summaries should not be evicted by later requests without SQL execution.");
                        return Task.CompletedTask;
                    });
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    public static Task LinqToDbSamplesShouldBeCapturedBySqlMonitoringAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
            {
                await context.EnableFeatureDirectlyAsync("Lombiq.HelpfulLibraries.Samples");

                const string requestPath = "/Lombiq.HelpfulLibraries.Samples/LinqToDbSamples/SimpleQuery";

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
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    public static Task SqlQueryMonitoringShouldCaptureRawQueryAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringScenarioAsync(
            executeTestAfterSetupAsync,
            browser,
            requestPathMethod: context => context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(controller => controller.RawQuery()),
            entry =>
                entry.CommandText.ContainsOrdinalIgnoreCase("SELECT") &&
                entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "The raw SQL query should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureRawExecuteNonQueryAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringScenarioAsync(
            executeTestAfterSetupAsync,
            browser,
            requestPathMethod: context => context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
                controller => controller.RawExecuteNonQuery()),
            entry =>
                entry.CommandText.ContainsOrdinalIgnoreCase("DELETE") &&
                entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "The raw SQL non-query command should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureCustomSessionQueryAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringScenarioAsync(
            executeTestAfterSetupAsync,
            browser,
            requestPathMethod: context => context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
                controller => controller.CustomSessionQuery()),
            entry => entry.CommandText.ContainsOrdinalIgnoreCase("ContentItemIndex"),
            "Queries executed through a manually created YesSql session should be captured.");

    public static Task SqlQueryMonitoringShouldCaptureDirectConnectionQueryAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        ExecuteSqlMonitoringScenarioAsync(
            executeTestAfterSetupAsync,
            browser,
            requestPathMethod: context => context.GetRelativeUrlOfAction<SqlQueryMonitoringScenarioController>(
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
        try
        {
            await assertionAsync();
            throw new ConfigurationErrorsException(failureMessage);
        }
        catch (TException exception)
        {
            assertException(exception);
        }
    }

    private static Task ExecuteSqlMonitoringTestAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task>? changeConfigurationAsync = null) =>
        executeTestAfterSetupAsync(
            testAsync,
            browser,
            async configuration =>
            {
                configuration.SqlQueryMonitoringConfiguration.EnableSqlQueryMonitoringCollection = true;

                if (changeConfigurationAsync != null)
                {
                    await changeConfigurationAsync(configuration);
                }
            });

    private static Task ExecuteSqlMonitoringScenarioAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser,
        Func<UITestContext, string> requestPathMethod,
        Predicate<SqlQueryExecutionEntry> executionPredicate,
        string assertionMessage) =>
        ExecuteSqlMonitoringTestAsync(
            executeTestAfterSetupAsync,
            async context =>
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
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);
}
