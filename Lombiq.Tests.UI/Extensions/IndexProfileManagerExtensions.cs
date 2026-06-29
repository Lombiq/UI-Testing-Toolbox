using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Elasticsearch;
using OrchardCore.Elasticsearch.Core.Recipes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchardCore.Indexing;

public static class IndexProfileManagerExtensions
{
    /// <summary>
    /// Rebuilds all Elasticsearch indexes. It's similar to <see cref="ElasticsearchIndexRebuildStep"/>, but the index
    /// isn't reset first, as that's not necessary during test initialization.
    /// </summary>
    public static async Task RebuildElasticsearchIndexesAsync(
        this IIndexProfileManager indexProfileManager,
        IServiceProvider serviceProvider)
    {
        var indexes = await indexProfileManager.GetByProviderAsync(ElasticsearchConstants.ProviderName);
        var indexManagers = new Dictionary<string, IIndexManager>();

        foreach (var index in indexes)
        {
            if (!indexManagers.TryGetValue(index.ProviderName, out var indexManager))
            {
                indexManager = serviceProvider.GetKeyedService<IIndexManager>(index.ProviderName);
                indexManagers[index.ProviderName] = indexManager;
            }

            if (indexManager is null) continue;

            await indexProfileManager.UpdateAsync(index);

            if (!await indexManager.ExistsAsync(index.IndexFullName))
            {
                await indexManager.CreateAsync(index);
            }
            else
            {
                await indexManager.RebuildAsync(index);
            }

            await indexProfileManager.SynchronizeAsync(index);
        }
    }
}
