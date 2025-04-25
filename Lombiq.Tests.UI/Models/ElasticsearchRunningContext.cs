using Elasticsearch.Net;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Indexing;
using OrchardCore.Search.Elasticsearch.Core.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(Guid Id, string Prefix)
{
    /// <summary>
    /// Gets the expression that refers to all indexes that starts with <see cref="Prefix"/>. This should only be used
    /// with <see cref="IElasticClient"/>, because the OrchardCore-specific services automatically apply the prefix from
    /// configuration so it would result in double prefixing.
    /// </summary>
    private IndexName LowLevelIndexName => Indices.Index($"{Prefix}_*");

    public Task BeforeTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async provider =>
        {
            var index = LowLevelIndexName;
            var cancellation = context.Configuration.TestCancellationToken;

            if (GetClient(provider) is not { } client)
            {
                throw new InvalidOperationException(
                    $"Couldn't resolve {nameof(IElasticClient)} while waiting for \"{index}\".");
            }

            (await client.Indices.FlushAsync(index, ct: cancellation)).ThrowIfFailed($"flush index \"{index}\"");
            (await client.Indices.RefreshAsync(index, ct: cancellation)).ThrowIfFailed($"refresh index \"{index}\"");

            // Elasticserch indexing sometimes takes longer, and the testing starts before it finishes. To prevent that,
            // we are checking if all of the indexing tasks are finished.
            var elasticIndexManager = provider.GetRequiredService<ElasticIndexManager>();
            var settingsService = provider.GetRequiredService<ElasticIndexSettingsService>();
            var indexingTaskManager = provider.GetRequiredService<IIndexingTaskManager>();
            var indexSettings = await settingsService.GetSettingsAsync();
            var exactIndexName = indexSettings.FirstOrDefault()?.IndexName;

            const int batchSize = 1000;
            long lastTaskId = 0;
            bool hasTask = true;

            // We are getting the last indexing task (regardless of the state). This function works like a cursor, so
            // there is no way to get directly the last task in the list. Since we have to give a "count" parameter, we
            // are retrieving the indexing tasks by batches of 1000. Then if there is no more, we get the last one.
            if (exactIndexName != null)
            {
                // We want to set "hasTask" inside the loop.
#pragma warning disable S1994 // "for" loop increment clauses should modify the loops' counters
                for (var startIndex = 0; hasTask; startIndex += batchSize)
                {
                    var lastTask = (await indexingTaskManager.GetIndexingTasksAsync(startIndex, batchSize))
                        .LastOrDefault();

                    hasTask = lastTask != null;

                    if (hasTask)
                    {
                        lastTaskId = lastTask.Id;
                    }
                }
#pragma warning restore S1994 // "for" loop increment clauses should modify the loops' counters

                long? lastFinishedTaskId = null;

                var timeout = TimeSpan.FromSeconds(60);
                var stopWatch = Stopwatch.StartNew();

                // We have the id of the last indexing task that should happen, so we are waiting here for that task to
                // complete, since "GetLastTaskId()" returns only completed tasks.
                while (lastTaskId > lastFinishedTaskId || lastFinishedTaskId == null)
                {
                    IsTimeout(stopWatch, timeout);

                    lastFinishedTaskId = await TryGetLastTaskIdAsync(elasticIndexManager, exactIndexName);

                    // The indexing takes a couple of seconds, so there is no need to check them so fast: we are adding
                    // a delay.
                    await Task.Delay(500, cancellation);
                }

                stopWatch.Stop();
            }
        });

    /// <summary>
    /// Asking for the last task ID can throw an exception if the underlying value is not initialized yet. This method
    /// catches the exception and returns null instead so it can be safely retried.
    /// </summary>
    private static async Task<long?> TryGetLastTaskIdAsync(ElasticIndexManager elasticIndexManager, string indexName)
    {
        try
        {
            return await elasticIndexManager.GetLastTaskId(indexName);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void IsTimeout(Stopwatch stopWatch, TimeSpan timeout)
    {
        if (stopWatch.Elapsed > timeout)
        {
            stopWatch.Stop();
            throw new TimeoutException($"Last finished tasked id did not match with last task id within {timeout}.");
        }
    }

    public Task AfterTestAsync(UITestContext context) =>
        context?.Application?.Services is { } ? AfterTestInnerAsync(context) : Task.CompletedTask;

    private async Task AfterTestInnerAsync(UITestContext context)
    {
        try
        {
            await context.Application.UsingScopeAsync(provider =>
                WithPrefixElasticsearchIndexCleanupFinallyAsync(provider, context, LowLevelIndexName));
        }
        catch (Exception inner)
        {
            context.Scope?.AtataContext?.Log?.Error(inner.ToString());
        }
    }

    [SuppressMessage(
        "Usage",
        "MA0040:Forward the CancellationToken parameter to methods that take one",
        Justification = "Cleanup code has no viable cancellation token because even failed tests should be cleaned up.")]
    private static async Task WithPrefixElasticsearchIndexCleanupFinallyAsync(
        IServiceProvider provider,
        UITestContext context,
        IndexName index)
    {
        static async Task<bool> CheckIfIndexExistsAsync(IElasticClient client, IndexName index)
        {
            var indices = (await client.Indices.GetAsync(index, ct: CancellationToken.None))
                .ThrowIfFailed($"query index \"{index}\"");
            return indices.Indices.Count > 0;
        }

        if (GetClient(provider) is not { } client)
        {
            throw new InvalidOperationException(
                $"Couldn't resolve {nameof(IElasticClient)} while attempting to clean up \"{index}\".");
        }

        if (!await CheckIfIndexExistsAsync(client, index))
        {
            context.Scope?.AtataContext?.Log?.Warn("No Elasticsearch indexes were found.");
            return;
        }

        var deleteRequest = new DeleteIndexRequest(index) { ExpandWildcards = ExpandWildcards.All };
        (await client.Indices.DeleteAsync(deleteRequest)).ThrowIfFailed($"delete index \"{index}\"");

        if (await CheckIfIndexExistsAsync(client, index))
        {
            throw new InvalidOperationException($"Couldn't delete indexes for \"{index.Name}\".");
        }
    }

    private static IElasticClient GetClient(IServiceProvider provider) =>
        provider.GetService<IElasticClient>() ?? provider.GetService<ElasticClient>();
}
