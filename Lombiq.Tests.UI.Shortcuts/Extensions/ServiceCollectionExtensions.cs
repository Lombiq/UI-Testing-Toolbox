using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Lombiq.Tests.UI.Shortcuts.Services;
using Nest;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds test-specific implementations for the Elasticsearch wrapper services.
    /// </summary>
    public static IServiceCollection AddTestPrefixedElasticsearchWrapperServices(this IServiceCollection services)
    {
        TestPrefixedElasticsearchIndexManager.AddService(services);

        // We call this extension to add the other required singleton services, if they are not yet registered.
        return services.HasImplementationsOf<IElasticClient>()
            ? services
            : services.AddDefaultElasticsearchWrapperServices();
    }
}
