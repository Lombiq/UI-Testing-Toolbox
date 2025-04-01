using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Search.Elasticsearch;
using OrchardCore.Search.Elasticsearch.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class TestPrefixedElasticsearchIndexManager : IElasticsearchIndexManager
{
    private readonly ElasticIndexManager _manager;
    private readonly IShellConfiguration _shellConfiguration;

    public TestPrefixedElasticsearchIndexManager(ElasticIndexManager manager, IShellConfiguration shellConfiguration)
    {
        _manager = manager;
        _shellConfiguration = shellConfiguration;
    }

    public Task<bool> DeleteIndex(string indexName) =>
        _manager.DeleteIndex(GetFullIndexName(indexName));

    public Task<ElasticTopDocs> SearchAsync(string indexName, QueryContainer query, List<ISort> sort, int from, int size) =>
        _manager.SearchAsync(GetFullIndexName(indexName), query, sort, from, size);

    public Task<bool> ExistsAsync(string indexName) =>
        _manager.ExistsAsync(GetFullIndexName(indexName));

    private string GetFullIndexName(string name) =>
        GetFullIndexName(name, _shellConfiguration);

    public static string GetFullIndexName(string name, IShellConfiguration shellConfiguration)
    {
        var prefix = TestPrefixedElasticsearchIndexStep.GetNormalizedPrefixFromConfiguration(shellConfiguration);
        var hasPrefix = !string.IsNullOrWhiteSpace(prefix);

        return hasPrefix && !name.StartsWithOrdinal(prefix + '-')
            ? $"{prefix}-{name}"
            : name;
    }

    public static void AddService(IServiceCollection services)
    {
        services.AddScoped<IElasticsearchIndexManager, TestPrefixedElasticsearchIndexManager>();
        services.AddScoped<IElasticsearchIndexingService, TestPrefixedElasticsearchIndexingService>();
    }
}
