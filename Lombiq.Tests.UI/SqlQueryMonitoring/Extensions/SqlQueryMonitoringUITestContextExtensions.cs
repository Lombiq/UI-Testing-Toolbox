using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.SqlQueryMonitoring.Helpers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring.Extensions;

public static class SqlQueryMonitoringUITestContextExtensions
{
    private const string NoSqlMonitoringSummaryCapturedMessage =
        "No SQL query monitoring summary was captured. Ensure the page has finished loading, executes SQL commands, " +
        "and that SQL query monitoring is enabled.";

    /// <summary>
    /// Executes assertions on the SQL query monitoring summary recorded for the current request only.
    /// Does not wait for follow-up requests and does not combine multiple request summaries.
    /// </summary>
    /// <param name="assertSummaryAsync">
    /// The assertion logic to run on the monitoring summary. If <see langword="null"/> then the assertion supplied in
    /// the context will be used.
    /// </param>
    public static async Task AssertSqlQueryMonitoringAsync(
        this UITestContext context,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync = null)
    {
        var summary = await context.GetCurrentRequestSqlQueryMonitoringSummaryAsync();
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
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync();
        var store = await context.GetSqlQueryMonitoringStoreAsync(context.TenantName)
            ?? throw new InvalidOperationException(NoSqlMonitoringSummaryCapturedMessage);

        var summaries = await CollectFollowUpSummariesAsync(context, store, summary);

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
        var (expectedPathAndQuery, expectedPath) = ParseExpectedRequestPath(requestPathOrUrl);
        var summary = await context.GetLatestSqlQueryMonitoringSummaryAsync(
            tenant: context.TenantName,
            expectedPathAndQuery,
            expectedPath,
            requestMethod);
        await AssertSqlQueryMonitoringSummaryAsync(context, summary, assertSummaryAsync);
    }

    private static Task AssertSqlQueryMonitoringSummaryAsync(
        UITestContext context,
        SqlQueryMonitoringSummary summary,
        Func<SqlQueryMonitoringSummary, Task> assertSummaryAsync)
    {
        var configuration = context.Configuration.SqlQueryMonitoringConfiguration;
        SqlQueryMonitoringHelpers.WriteSqlQueryMonitoringCounters(context.Configuration.TestOutputHelper, summary, configuration);

        var assertTask = (assertSummaryAsync ?? configuration.AssertSqlQueryMonitoringSummaryAsync)?
            .Invoke(summary);
        return assertTask ?? Task.CompletedTask;
    }

    private static async Task<SqlQueryMonitoringSummary> GetCurrentRequestSqlQueryMonitoringSummaryAsync(this UITestContext context)
    {
        var currentUri = context.GetCurrentUri();
        var expectedTenantName = NormalizeTenantName(context.TenantName);
        var store = await context.GetSqlQueryMonitoringStoreAsync(context.TenantName)
            ?? throw new InvalidOperationException(NoSqlMonitoringSummaryCapturedMessage);

        if (TryRemoveMostRecentMatchingRequest(
            store,
            expectedTenantName,
            currentUri.PathAndQuery,
            currentUri.AbsolutePath,
            expectedMethod: null,
            out var summary))
        {
            return summary;
        }

        throw new InvalidOperationException(
            $"No SQL query monitoring summary was captured for \"{currentUri.PathAndQuery}\". Ensure the request has " +
            $"finished and that SQL query monitoring is enabled. Summaries: {JsonSerializer.Serialize(store.ReadonlySummaries())}");
    }

    /// <summary>
    /// Returns the SQL query monitoring summary recorded for the most recent request.
    /// </summary>
    private static Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(this UITestContext context)
    {
        var currentUri = context.GetCurrentUri();
        return GetLatestSqlQueryMonitoringSummaryAsync(
            context,
            tenant: context.TenantName,
            expectedPathAndQuery: currentUri.PathAndQuery,
            expectedPath: currentUri.AbsolutePath,
            requestMethod: null);
    }

    private static async Task<SqlQueryMonitoringSummary> GetLatestSqlQueryMonitoringSummaryAsync(
        this UITestContext context,
        string tenant,
        string expectedPathAndQuery,
        string expectedPath,
        string requestMethod)
    {
        var expectedTenantName = NormalizeTenantName(tenant);
        var sqlMonitoringConfiguration = context.Configuration.SqlQueryMonitoringConfiguration;
        var store = await context.GetSqlQueryMonitoringStoreAsync(tenant)
            ?? throw new InvalidOperationException(NoSqlMonitoringSummaryCapturedMessage);

        var deadline = DateTime.UtcNow + sqlMonitoringConfiguration.SummaryLookupTimeout;

        // Even for the main page request there can be a short race: the assertion may run before the summary is
        // enqueued by the middleware. Wait briefly for the expected request summary.
        while (DateTime.UtcNow < deadline)
        {
            if (TryRemoveMostRecentMatchingRequest(
                store,
                expectedTenantName,
                expectedPathAndQuery,
                expectedPath,
                requestMethod,
                out var summary))
            {
                return summary;
            }

            await Task.Delay(sqlMonitoringConfiguration.SummaryLookupInterval, context.Configuration.TestCancellationToken);
        }

        var requestDescription = string.IsNullOrWhiteSpace(requestMethod)
            ? expectedPathAndQuery
            : $"{requestMethod} {expectedPathAndQuery}";

        throw new InvalidOperationException(
            $"No SQL query monitoring summary was captured for \"{requestDescription}\". Ensure the request has " +
            $"finished and that SQL query monitoring is enabled. Summaries: {JsonSerializer.Serialize(store.ReadonlySummaries())}");
    }

    private static async Task<ISqlQueryMonitoringStore> GetSqlQueryMonitoringStoreAsync(
        this UITestContext context,
        string tenant)
    {
        ISqlQueryMonitoringStore store = null;

        await context.Application.UsingScopeAsync(
            serviceProvider =>
            {
                store = serviceProvider.GetService<ISqlQueryMonitoringStore>();
                return Task.CompletedTask;
            },
            tenant);

        return store;
    }

    // Collects additional summaries produced shortly after the initial summary (for example by async browser calls).
    private static async Task<List<SqlQueryMonitoringSummary>> CollectFollowUpSummariesAsync(
        UITestContext context,
        ISqlQueryMonitoringStore store,
        SqlQueryMonitoringSummary initialSummary)
    {
        var sqlMonitoringConfiguration = context.Configuration.SqlQueryMonitoringConfiguration;
        var summaries = new List<SqlQueryMonitoringSummary> { initialSummary };
        var now = DateTime.UtcNow;
        var deadline = now + sqlMonitoringConfiguration.SummaryLookupTimeout;
        var quietDeadline = now + sqlMonitoringConfiguration.FollowUpSummaryQuietPeriod;
        var pollingInterval = sqlMonitoringConfiguration.SummaryLookupInterval;

        // Keep polling until the hard timeout is reached or no new follow-up summary arrives before the quiet period
        // ends.
        while (now < deadline && now < quietDeadline)
        {
            var capturedFollowUpSummary = false;

            // Drain all follow-up summaries that are already available before waiting again.
            while (TryRemoveMostRecentFollowUp(store, initialSummary.TenantName, initialSummary.CompletedUtc, out var additionalSummary))
            {
                if (additionalSummary?.Executions.Count > 0)
                {
                    summaries.Add(additionalSummary);
                    capturedFollowUpSummary = true;
                }
            }

            if (capturedFollowUpSummary)
            {
                quietDeadline = DateTime.UtcNow + sqlMonitoringConfiguration.FollowUpSummaryQuietPeriod;
            }

            var stopAt = deadline < quietDeadline ? deadline : quietDeadline;
            var remainingUntilStop = stopAt - DateTime.UtcNow;
            var delay = remainingUntilStop < pollingInterval ? remainingUntilStop : pollingInterval;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, context.Configuration.TestCancellationToken);
            }

            now = DateTime.UtcNow;
        }

        return summaries;
    }

    // Removes and returns the newest summary that matches the explicit request selector (tenant + path/query + method).
    // Used by request-specific assertions where mismatches must not silently pass.
    private static bool TryRemoveMostRecentMatchingRequest(
        ISqlQueryMonitoringStore store,
        string expectedTenantName,
        string expectedPathAndQuery,
        string expectedPath,
        string expectedMethod,
        out SqlQueryMonitoringSummary summary) =>
        TryRemoveMostRecentMatching(
            store,
            candidate =>
                TenantNameMatches(candidate?.TenantName, expectedTenantName) &&
                RequestPathMatches(candidate?.RequestPath, expectedPathAndQuery, expectedPath) &&
                RequestMethodMatches(candidate?.RequestMethod, expectedMethod),
            out summary);

    // Removes and returns the newest tenant-scoped summary produced at or after the initial matched summary.
    // Used during follow-up polling to avoid merging stale summaries from earlier requests.
    private static bool TryRemoveMostRecentFollowUp(
        ISqlQueryMonitoringStore store,
        string expectedTenantName,
        DateTimeOffset minimumCompletedUtc,
        out SqlQueryMonitoringSummary summary) =>
        TryRemoveMostRecentMatching(
            store,
            candidate =>
                TenantNameMatches(candidate?.TenantName, expectedTenantName) &&
                candidate?.CompletedUtc >= minimumCompletedUtc,
            out summary);

    private static bool TryRemoveMostRecentMatching(
        ISqlQueryMonitoringStore store,
        Predicate<SqlQueryMonitoringSummary> predicate,
        out SqlQueryMonitoringSummary summary) =>
        store.TryRemoveMostRecentMatching(predicate, out summary);

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

        // If the caller specified a query string, this must be an exact path+query match to avoid selecting an
        // unrelated request with the same path but different query parameters.
        if (expectedPathAndQuery.Contains('?', StringComparison.Ordinal))
        {
            return false;
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
            tenantName: latestCompletedSummary.TenantName,
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
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPathOrUrl);

        if (Uri.TryCreate(requestPathOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            return (absoluteUri.PathAndQuery, absoluteUri.AbsolutePath);
        }

        var expectedPathAndQuery = requestPathOrUrl;
        if (!expectedPathAndQuery.StartsWith('/'))
        {
            expectedPathAndQuery = "/" + expectedPathAndQuery;
        }

        var queryStringStartIndex = expectedPathAndQuery.IndexOf('?', StringComparison.Ordinal);
        var expectedPath = queryStringStartIndex >= 0
            ? expectedPathAndQuery[..queryStringStartIndex]
            : expectedPathAndQuery;

        return (expectedPathAndQuery, expectedPath);
    }

    private static string NormalizeTenantName(string tenantName) =>
        string.IsNullOrWhiteSpace(tenantName) || tenantName.StartsWith('!')
            ? ShellSettings.DefaultShellName
            : tenantName;

    private static bool TenantNameMatches(string actualTenantName, string expectedTenantName) =>
        string.Equals(
            NormalizeTenantName(actualTenantName),
            NormalizeTenantName(expectedTenantName),
            StringComparison.OrdinalIgnoreCase);
}
