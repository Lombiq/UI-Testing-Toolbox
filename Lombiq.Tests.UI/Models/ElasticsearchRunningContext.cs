using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Indexing;
using OrchardCore.Search.Elasticsearch.Core.Models;
using OrchardCore.Search.Elasticsearch.Core.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(Guid Id, string Prefix)
{
    /// <summary>
    /// Gets the expression that refers to all indexes that start with <see cref="Prefix"/>. This should only be used
    /// with <see cref="ElasticsearchClient"/>, because the OrchardCore-specific services automatically apply the prefix from
    /// configuration so it would result in double prefixing.
    /// </summary>
    private IndexName LowLevelIndexName => Indices.Index($"{Prefix}_*");

    // Elasticsearch indexing sometimes takes longer, and the testing starts before indexing finishes. To prevent that,
    // we are checking if all indexing tasks are finished.
    public Task BeforeTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async provider =>
        {
            var index = LowLevelIndexName;
            var testCancellationToken = context.Configuration.TestCancellationToken;
            var client = GetClient(provider, index);

            (await client.Indices.FlushAsync(index, cancellationToken: testCancellationToken)).ThrowIfFailed($"flush index \"{index}\"");
            (await client.Indices.RefreshAsync(index, cancellationToken: testCancellationToken)).ThrowIfFailed($"refresh index \"{index}\"");

            var exactIndexName = (await provider.GetRequiredService<ElasticIndexSettingsService>().GetSettingsAsync())
                .FirstOrDefault()
                ?.IndexName;

            if (exactIndexName == null) return;

            var timeout = TimeSpan.FromSeconds(60);

            using var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
            using var jointCancellationTokenSource = CancellationTokenSource
                .CreateLinkedTokenSource(testCancellationToken, timeoutCancellationTokenSource.Token);
            var jointCancellationToken = jointCancellationTokenSource.Token;

            var elasticIndexManager = provider.GetRequiredService<ElasticsearchIndexManager>();

            long? lastFinishedTaskId = null;

            try
            {
                var lastTaskId = await GetLastTaskIdAsync(provider.GetRequiredService<IIndexingTaskManager>());

                // We have the ID of the last indexing task that should happen, so we are waiting here for that task to
                // complete, since "GetLastTaskId()" returns only completed tasks.
                while (lastFinishedTaskId < lastTaskId || lastFinishedTaskId == null)
                {
                    jointCancellationToken.ThrowIfCancellationRequested();

                    lastFinishedTaskId = await TryGetLastFinishedTaskIdAsync(elasticIndexManager, exactIndexName);

                    // The indexing takes a couple of seconds, so there is no need to check them so fast: we are adding
                    // a delay.
                    await Task.Delay(500, jointCancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                throw new TaskCanceledException(
                    "Elasticsearch indexing wasn't finished due to " +
                        $"{(testCancellationToken.IsCancellationRequested ? "the test being canceled" : $"it not completing within {timeout}")}.",
                    ex,
                    jointCancellationToken);
            }
        });

    public Task AfterTestAsync(UITestContext context) =>
        context?.Application?.Services is { } ? AfterTestInnerAsync(context) : Task.CompletedTask;

    private static async Task<long> GetLastTaskIdAsync(IIndexingTaskManager indexingTaskManager)
    {
        const int batchSize = 1000;
        long lastTaskId = 0;
        bool hasTask = true;

        // We are getting the last indexing task (regardless of the state). This function works like a cursor, so
        // there is no way to get the last task in the list directly. Since we have to provide a "count" parameter,
        // we are retrieving the indexing tasks by batches of 1000. Then if there are no more, we get the last one.
        // We want to set "hasTask" inside the loop.
#pragma warning disable S1994 // "for" loop increment clauses should modify the loops' counters
        for (var startIndex = 0; hasTask; startIndex += batchSize)
        {
            var lastTask = (await indexingTaskManager.GetIndexingTasksAsync(startIndex, batchSize, category: null))
                .LastOrDefault();

            hasTask = lastTask != null;

            if (hasTask)
            {
                lastTaskId = lastTask.Id;
            }
        }
#pragma warning restore S1994 // "for" loop increment clauses should modify the loops' counters

        return lastTaskId;
    }

    /// <summary>
    /// Asking for the last task ID can throw an exception if the underlying value is not initialized yet. This method
    /// catches the exception and returns null instead so it can be safely retried.
    /// </summary>
    private static async Task<long?> TryGetLastFinishedTaskIdAsync(ElasticsearchIndexManager elasticIndexManager, string indexName)
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
        static async Task<bool> CheckIfIndexExistsAsync(ElasticsearchClient client, IndexName index)
        {
            var indices = (await client.Indices.GetAsync(index, cancellationToken: CancellationToken.None))
                .ThrowIfFailed($"query index \"{index}\"");
            return indices.Indices.Count > 0;
        }

        var client = GetClient(provider, index);

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

    private static ElasticsearchClient GetClient(IServiceProvider provider, IndexName index) =>
        (provider.GetService<IElasticsearchClientFactory>() is { } factory
            ? factory.Create(new ElasticsearchConnectionOptions())
            : provider.GetService<ElasticsearchClient>()) ??
        throw new InvalidOperationException(
            $"Couldn't resolve {nameof(ElasticsearchClient)} while waiting for \"{index}\".");
}
