using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Search.Elasticsearch.Core.Models;
using OrchardCore.Search.Elasticsearch.Core.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class TestPrefixedElasticsearchIndexingService : IElasticsearchIndexingService
{
    private readonly ElasticIndexingService _service;
    private readonly IShellConfiguration _shellConfiguration;

    public TestPrefixedElasticsearchIndexingService(ElasticIndexingService service, IShellConfiguration shellConfiguration)
    {
        _service = service;
        _shellConfiguration = shellConfiguration;
    }

    public Task RebuildIndexAsync(ElasticIndexSettings elasticIndexSettings) =>
        _service.RebuildIndexAsync(UpdateFullIndexSettings(elasticIndexSettings));

    public Task ProcessContentItemsAsync(params string[] indexNames) =>
        _service.ProcessContentItemsAsync(indexNames.Select(GetFullIndexName).ToArray());

    public Task CreateIndexAsync(ElasticIndexSettings elasticIndexSettings) =>
        _service.CreateIndexAsync(UpdateFullIndexSettings(elasticIndexSettings));

    private string GetFullIndexName(string name) =>
        TestPrefixedElasticsearchIndexManager.GetFullIndexName(name, _shellConfiguration);

    private ElasticIndexSettings UpdateFullIndexSettings(ElasticIndexSettings settings)
    {
        settings.IndexName = GetFullIndexName(settings.IndexName);
        return settings;
    }
}
