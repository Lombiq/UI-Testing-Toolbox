using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;

public static class SqlQueryMonitoringUITestContextExtensions
{
    /// <summary>
    /// Executes assertions on the SQL query monitoring summary recorded for the most recent request.
    /// </summary>
    /// <param name="assertSummaryAsync">
    /// The assertion logic to run on the monitoring summary. If <see langword="null"/> then the assertion supplied in
    /// the context will be used.
    /// </param>
    public static async Task AssertSqlQueryMonitoringAsync(
        this UITestContext context,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync = null)
    {
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
        await AssertSqlQueryMonitoringSummaryAsync(context, summary, assertSummaryAsync);
    }

    /// <summary>
    /// Executes assertions on SQL monitoring while also waiting briefly for follow-up requests (for example, async
    /// browser-triggered API calls) to finish and be captured.
    /// </summary>
    public static async Task AssertSqlQueryMonitoringIncludingFollowUpRequestsAsync(
        this UITestContext context,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync = null)
    {
        var sqlMonitoringConfiguration = context.Configuration.SqlQueryMonitoringConfiguration;
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
        var store = await context.GetSqlQueryMonitoringStoreAsync(context.TenantName)
            ?? throw new InvalidOperationException(
                "No SQL query monitoring summary was captured. Ensure the page has finished loading, executes SQL " +
                "commands, and that SQL query monitoring is enabled.");

        var summaries = new List<SqlQueryMonitoringSummary> { summary };
        var deadline = DateTime.UtcNow + sqlMonitoringConfiguration.SummaryLookupTimeout;
        var lastSummaryCapturedUtc = DateTime.UtcNow;

        // After we captured the initial page/request summary, keep polling briefly so client-side follow-up requests
        // (for example fetch/XHR calls triggered right after navigation) can be included in one combined assertion.
        while (DateTime.UtcNow < deadline)
        {
            if (TryDequeueMostRecentAvailable(store, out var additionalSummary))
            {
                if (additionalSummary?.Executions.Count > 0)
                {
                    summaries.Add(additionalSummary);
                    lastSummaryCapturedUtc = DateTime.UtcNow;
                }

                continue;
            }

            if (DateTime.UtcNow - lastSummaryCapturedUtc >= sqlMonitoringConfiguration.FollowUpSummaryQuietPeriod) break;

            await Task.Delay(sqlMonitoringConfiguration.SummaryLookupInterval, context.Configuration.TestCancellationToken);
        }

        var summaryToAssert = summaries.Count == 1
            ? summary
            : CreateCombinedSummary(context.GetCurrentUri().PathAndQuery, summaries);

        await AssertSqlQueryMonitoringSummaryAsync(context, summaryToAssert, assertSummaryAsync);
    }

    /// <summary>
    /// Executes assertions on the SQL query monitoring summary recorded for a specific request path.
    /// </summary>
    /// <param name="requestPathOrUrl">
    /// The request path/query (for example, <c>/api/items?page=1</c>) or an absolute URL.
    /// </param>
    /// <param name="requestMethod">
    /// Optional HTTP method filter (for example, <c>GET</c>).
    /// </param>
    /// <param name="assertSummaryAsync">
    /// The assertion logic to run on the monitoring summary. If <see langword="null"/> then the assertion supplied in
    /// the context will be used.
    /// </param>
    public static async Task AssertSqlQueryMonitoringForRequestAsync(
        this UITestContext context,
        string requestPathOrUrl,
        string requestMethod = null,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync = null)
    {
        if (string.IsNullOrWhiteSpace(requestPathOrUrl))
        {
            throw new ArgumentException("Request path or URL must be provided.", nameof(requestPathOrUrl));
        }

        var (expectedPathAndQuery, expectedPath) = ParseExpectedRequestPath(requestPathOrUrl);
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync(
            tenant: context.TenantName,
            expectedPathAndQuery,
            expectedPath,
            requestMethod);
        await AssertSqlQueryMonitoringSummaryAsync(context, summary, assertSummaryAsync);
    }

    private static async Task AssertSqlQueryMonitoringSummaryAsync(
        UITestContext context,
        SqlQueryMonitoringSummary summary,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync)
    {
        var configuration = context.Configuration.SqlQueryMonitoringConfiguration;
        configuration.WriteSqlQueryMonitoringCounters(context.Configuration.TestOutputHelper, summary);

        try
        {
            var assertTask = (assertSummaryAsync ?? configuration.AssertSqlQueryMonitoringSummaryAsync)?
                .Invoke(summary);
            await (assertTask ?? Task.CompletedTask);
        }
        catch (Exception exception)
        {
            throw new SqlQueryMonitoringAssertionException(summary, configuration, exception);
        }
    }

    /// <summary>
    /// Returns the SQL query monitoring summary recorded for the most recent request.
    /// </summary>
    private static Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(this UITestContext context) =>
        GetLatestSqlQueryMonitoringSummaryAsync(
            context,
            tenant: context.TenantName,
            expectedPathAndQuery: context.GetCurrentUri().PathAndQuery,
            expectedPath: context.GetCurrentUri().AbsolutePath,
            requestMethod: null);

    private static async Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(
        this UITestContext context,
        string tenant,
        string expectedPathAndQuery,
        string expectedPath,
        string requestMethod)
    {
        var sqlMonitoringConfiguration = context.Configuration.SqlQueryMonitoringConfiguration;
        var store = await context.GetSqlQueryMonitoringStoreAsync(tenant)
            ?? throw new InvalidOperationException(
                "No SQL query monitoring summary was captured. Ensure the page has finished loading, executes SQL " +
                "commands, and that SQL query monitoring is enabled.");

        var deadline = DateTime.UtcNow + sqlMonitoringConfiguration.SummaryLookupTimeout;
        // Even for the "main" page request there can be a short race: the assertion may run before the summary is
        // enqueued by middleware. Wait briefly for the expected request summary instead of immediately falling back.
        while (DateTime.UtcNow < deadline)
        {
            if (TryDequeueMostRecentMatchingRequest(
                store,
                expectedPathAndQuery,
                expectedPath,
                requestMethod,
                out var summary))
            {
                return summary;
            }

            await Task.Delay(sqlMonitoringConfiguration.SummaryLookupInterval, context.Configuration.TestCancellationToken);
        }

        if (TryDequeueMostRelevantFallback(store, out var fallbackSummary))
        {
            return fallbackSummary;
        }

        var requestDescription = string.IsNullOrWhiteSpace(requestMethod)
            ? expectedPathAndQuery
            : $"{requestMethod} {expectedPathAndQuery}";

        throw new InvalidOperationException(
            $"No SQL query monitoring summary was captured for \"{requestDescription}\". Ensure the request has " +
            "finished and that SQL query monitoring is enabled.");
    }

    private static async Task<ISqlQueryMonitoringStore> GetSqlQueryMonitoringStoreAsync(
        this UITestContext context,
        string tenant)
    {
        var store = context.Application.Services.GetService<ISqlQueryMonitoringStore>();

        if (store != null) return store;

        await context.Application.UsingScopeAsync(
            serviceProvider =>
            {
                store = serviceProvider.GetService<ISqlQueryMonitoringStore>();
                return Task.CompletedTask;
            },
            tenant);

        return store;
    }

    private static bool TryDequeueMostRecentMatchingRequest(
        ISqlQueryMonitoringStore store,
        string expectedPathAndQuery,
        string expectedPath,
        string expectedMethod,
        out SqlQueryMonitoringSummary summary)
    {
        if (store is not SqlQueryMonitoringStore concreteStore)
        {
            summary = null;
            return false;
        }

        return concreteStore.TryDequeueMostRecentMatching(
            candidate =>
                RequestPathMatches(candidate?.RequestPath, expectedPathAndQuery, expectedPath) &&
                RequestMethodMatches(candidate?.RequestMethod, expectedMethod),
            out summary);
    }

    private static bool TryDequeueMostRelevantFallback(
        ISqlQueryMonitoringStore store,
        out SqlQueryMonitoringSummary summary)
    {
        if (store is SqlQueryMonitoringStore concreteStore)
        {
            return concreteStore.TryDequeueMostRecentWithExecutions(out summary) ||
                concreteStore.TryDequeueMostRecent(out summary);
        }

        return store.TryDequeueMostRecentWithExecutions(out summary);
    }

    private static bool TryDequeueMostRecentAvailable(
        ISqlQueryMonitoringStore store,
        out SqlQueryMonitoringSummary summary) =>
        store is SqlQueryMonitoringStore concreteStore
            ? concreteStore.TryDequeueMostRecent(out summary)
            : store.TryDequeueMostRecentWithExecutions(out summary);

    private static bool RequestPathMatches(
        string requestPath,
        string expectedPathAndQuery,
        string expectedPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath)) return false;

        if (string.Equals(requestPath, expectedPathAndQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var queryStringStartIndex = requestPath.IndexOf('?', StringComparison.Ordinal);
        var requestPathWithoutQuery = queryStringStartIndex >= 0
            ? requestPath[..queryStringStartIndex]
            : requestPath;

        return string.Equals(requestPathWithoutQuery, expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequestMethodMatches(string requestMethod, string expectedMethod) =>
        string.IsNullOrWhiteSpace(expectedMethod) ||
        string.Equals(requestMethod, expectedMethod, StringComparison.OrdinalIgnoreCase);

    private static SqlQueryMonitoringSummary CreateCombinedSummary(
        string requestPath,
        List<SqlQueryMonitoringSummary> summaries)
    {
        var latestCompletedSummary = summaries
            .OrderByDescending(summary => summary.CompletedUtc)
            .First();

        return new SqlQueryMonitoringSummary(
            requestPath:
            $"{requestPath} (combined {summaries.Count} request summaries)",
            requestMethod: "MULTI",
            traceIdentifier: latestCompletedSummary.TraceIdentifier,
            completedUtc: latestCompletedSummary.CompletedUtc,
            executions: summaries
                .SelectMany(summary => summary.Executions)
                .ToList());
    }

    private static (string ExpectedPathAndQuery, string ExpectedPath) ParseExpectedRequestPath(string requestPathOrUrl)
    {
        if (Uri.TryCreate(requestPathOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            return (absoluteUri.PathAndQuery, absoluteUri.AbsolutePath);
        }

        var expectedPathAndQuery = requestPathOrUrl.StartsWith('/') ? requestPathOrUrl : "/" + requestPathOrUrl;
        var queryStringStartIndex = expectedPathAndQuery.IndexOf('?', StringComparison.Ordinal);
        var expectedPath = queryStringStartIndex >= 0
            ? expectedPathAndQuery[..queryStringStartIndex]
            : expectedPathAndQuery;

        return (expectedPathAndQuery, expectedPath);
    }
}
