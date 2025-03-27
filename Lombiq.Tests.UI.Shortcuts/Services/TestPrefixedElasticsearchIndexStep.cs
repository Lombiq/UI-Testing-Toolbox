using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.Search.Elasticsearch.Core.Models;
using OrchardCore.Search.Elasticsearch.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

/// <summary>
/// This recipe step creates an Elasticsearch index that may be prefixed from app settings using the string <see
/// cref="ConfigurationKey"/>. This setting should only be filled during testing and left empty during production, which
/// will reproduce the behavior of its stock Orchard Core counterpart.
/// </summary>
public sealed class TestPrefixedElasticsearchIndexStep : NamedRecipeStepHandler
{
    public const string ConfigurationKey = "Lombiq_Tests_UI_Shortcuts_ElasticsearchPrefix";

    private readonly ElasticIndexingService _elasticIndexingService;
    private readonly IElasticsearchIndexManager _elasticIndexManager;
    private readonly IShellConfiguration _shellConfiguration;

    public TestPrefixedElasticsearchIndexStep(
        ElasticIndexingService elasticIndexingService,
        IElasticsearchIndexManager elasticIndexManager,
        IShellConfiguration shellConfiguration)
        : base("ElasticIndexSettings")
    {
        _elasticIndexManager = elasticIndexManager;
        _shellConfiguration = shellConfiguration;
        _elasticIndexingService = elasticIndexingService;
    }

    protected override Task HandleAsync(RecipeExecutionContext context)
    {
        var prefix = GetNormalizedPrefixFromConfiguration(_shellConfiguration);
        var hasPrefix = !string.IsNullOrWhiteSpace(prefix);

        // The term "Indices" is supported to maintain compatibility with Orchard Core's ElasticIndexSettings.
        var indexes = context.Step["Indexes"] as JsonArray ??
                      context.Step["Indices"] as JsonArray ??
                      [];

        return indexes
            .SelectMany(index => index.ToObject<Dictionary<string, ElasticIndexSettings>>())
            .Select(pair => WithIndexName(pair.Value, hasPrefix ? $"{prefix}-{pair.Key}" : pair.Key))
            .ToAsyncEnumerable()
            .WhereNotAsync(settings => _elasticIndexManager.ExistsAsync(settings.IndexName))
            .ForEachAwaitAsync(_elasticIndexingService.CreateIndexAsync);
    }

    private static ElasticIndexSettings WithIndexName(ElasticIndexSettings settings, string name)
    {
        settings.IndexName = name;
        return settings;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Elasticsearch indexes are lowercase only.")]
    public static string GetNormalizedPrefixFromConfiguration(IShellConfiguration configuration) =>
        configuration[ConfigurationKey]?
            .ToLowerInvariant()
            .RegexReplace("[^a-z0-9]+", "-")
            .Trim('-');
}
