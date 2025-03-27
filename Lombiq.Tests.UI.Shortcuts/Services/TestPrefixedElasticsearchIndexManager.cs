using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Search.Elasticsearch;
using OrchardCore.Search.Elasticsearch.Core.Services;
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

    private string GetFullIndexName(string name)
    {
        var prefix = TestPrefixedElasticsearchIndexStep.GetNormalizedPrefixFromConfiguration(_shellConfiguration);
        var hasPrefix = !string.IsNullOrWhiteSpace(prefix);

        return hasPrefix ? $"{prefix}-{name}" : name;
    }

    public static void ReplaceServiceImplementation(IServiceCollection services)
    {
        services.RemoveImplementationsOf<IElasticsearchIndexManager>();
        services.AddScoped<IElasticsearchIndexManager, TestPrefixedElasticsearchIndexManager>();
    }
}
