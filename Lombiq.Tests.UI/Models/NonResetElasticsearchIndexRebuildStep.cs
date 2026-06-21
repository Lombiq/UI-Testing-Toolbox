using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Elasticsearch;
using OrchardCore.Elasticsearch.Core.Deployment;
using OrchardCore.Elasticsearch.Core.Recipes;
using OrchardCore.Indexing;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// This recipe step rebuilds an Elasticsearch index. It's identical to <see cref="ElasticsearchIndexRebuildStep"/>,
/// except the index isn't reset first.
/// </summary>
/// <remarks><para>
/// This variant is necessary to avoid errors like this:
/// [Error] OrchardCore.Elasticsearch.Core.Services.ElasticsearchDocumentIndexManager: There were issues updating
/// mappings in an Elasticsearch indexElastic.Transport.TransportException: Request failed to execute. Call: Status code
/// 404 from: PUT /{index}/_mapping. ServerError: Type: index_not_found_exception Reason: "no such index
/// [elasticsearchshouldwork-91dc6a1ea14ad882793fd90d5e1e3fb2aa5bc6d13695f7d398c238dccec64cbf_default_elasticsearchshouldwork]".
/// </para></remarks>
public sealed class NonResetElasticsearchIndexRebuildStep : NamedRecipeStepHandler
{
    private readonly IIndexProfileManager _indexProfileManager;
    private readonly IServiceProvider _serviceProvider;

    public NonResetElasticsearchIndexRebuildStep(
        IIndexProfileManager indexProfileManager,
        IServiceProvider serviceProvider)
        : base("elastic-index-rebuild")
    {
        _indexProfileManager = indexProfileManager;
        _serviceProvider = serviceProvider;
    }

    [SuppressMessage(
        "Critical Code Smell",
        "S3776:Cognitive Complexity of methods should not be too high",
        Justification = "This is a minimally altered version of a stock recipe step, so no major refactoring should be applied.")]
    protected override async Task HandleAsync(RecipeExecutionContext context)
    {
        var model = context.Step.ToObject<ElasticsearchIndexRebuildDeploymentStep>();

        if (model != null && (model.IncludeAll || model.Indices.Length > 0))
        {
            var indexes = model.IncludeAll
                ? await _indexProfileManager.GetByProviderAsync(ElasticsearchConstants.ProviderName)
                : (await _indexProfileManager.GetByProviderAsync(ElasticsearchConstants.ProviderName))
                .Where(x => model.Indices.Contains(x.IndexName));

            var indexManagers = new Dictionary<string, IIndexManager>();

            foreach (var index in indexes)
            {
                if (!indexManagers.TryGetValue(index.ProviderName, out var indexManager))
                {
                    indexManager = _serviceProvider.GetKeyedService<IIndexManager>(index.ProviderName);
                    indexManagers[index.ProviderName] = indexManager;
                }

                if (indexManager is null)
                {
                    continue;
                }

                // The "await _indexProfileManager.ResetAsync(index);" has been removed from here, otherwise identical
                // to the original.
                await _indexProfileManager.UpdateAsync(index);

                if (!await indexManager.ExistsAsync(index.IndexFullName))
                {
                    await indexManager.CreateAsync(index);
                }
                else
                {
                    await indexManager.RebuildAsync(index);
                }

                await _indexProfileManager.SynchronizeAsync(index);
            }
        }
    }
}
