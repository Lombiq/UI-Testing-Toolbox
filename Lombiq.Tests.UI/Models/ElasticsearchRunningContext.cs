using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Indexing;
using OrchardCore.Indexing.Core;
using OrchardCore.Recipes.Models;
using OrchardCore.Elasticsearch.Core.Deployment;
using OrchardCore.Elasticsearch.Core.Models;
using OrchardCore.Elasticsearch.Core.Recipes;
using OrchardCore.Elasticsearch.Core.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(string Prefix)
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
        context.Application.UsingScopeServiceProviderAsync(async provider =>
        {
            if (provider.GetService<IIndexProfileManager>() is { } indexProfileManager)
            {
                await RebuildAllIndexesAsync(indexProfileManager, provider);
            }

            if (provider.GetService<ContentIndexingService>() is { } indexingService)
            {
                await indexingService.ProcessRecordsForAllIndexesAsync();
            }
        });

    public async Task AfterTestAsync(UITestContext context)
    {
        try
        {
            if (context?.Application?.Services != null)
            {
                await context.Application.UsingScopeServiceProviderAsync(provider =>
                    WithPrefixElasticsearchIndexCleanupFinallyAsync(provider, context, LowLevelIndexName));
            }
        }
        catch (Exception inner)
        {
            context?.Scope?.AtataContext?.Log?.Error(inner.ToString());
        }
    }

    [SuppressMessage(
        "Usage",
        "MA0040:Forward the CancellationToken parameter to methods that take one",
        Justification = "Cleanup code has no viable cancellation token because even failed tests should be cleaned up.")]
    private async Task WithPrefixElasticsearchIndexCleanupFinallyAsync(
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

        var client = GetClient(provider);

        if (!await CheckIfIndexExistsAsync(client, index))
        {
            context.Scope?.AtataContext?.Log?.Warn("No Elasticsearch indexes were found.");
            return;
        }

        await client.DeleteAllIndexesAsync(Prefix);
        if (await CheckIfIndexExistsAsync(client, index))
        {
            throw new InvalidOperationException($"Couldn't delete indexes for \"{index}\".");
        }
    }

    private static ElasticsearchClient GetClient(IServiceProvider provider)
    {
        if (provider.GetService<ElasticsearchClient>() is { } existingClient)
        {
            return existingClient;
        }

        if (provider.GetService<IElasticsearchClientFactory>() is { } factory &&
            factory.Create(new ElasticsearchConnectionOptions()) is { } factoryClient)
        {
            return factoryClient;
        }

        throw new InvalidOperationException(
            $"Couldn't resolve {nameof(ElasticsearchClient)}.");
    }

    private static Task RebuildAllIndexesAsync(
        IIndexProfileManager indexProfileManager,
        IServiceProvider serviceProvider)
    {
        var step = new ElasticsearchIndexRebuildStep(indexProfileManager, serviceProvider);
        var model = new ElasticsearchIndexRebuildDeploymentStep { IncludeAll = true };
        var context = new RecipeExecutionContext
        {
            ExecutionId = Guid.NewGuid().ToString(),
            Name = "elastic-index-rebuild",
            Step = (JsonObject)JsonSerializer.SerializeToNode(model),
        };

        return step.ExecuteAsync(context);
    }
}
