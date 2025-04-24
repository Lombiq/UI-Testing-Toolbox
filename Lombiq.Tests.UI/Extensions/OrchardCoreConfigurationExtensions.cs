using Lombiq.Tests.UI.Services;
using OrchardCore.Search.Elasticsearch.Core.Recipes;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class OrchardCoreConfigurationExtensions
{
    /// <summary>
    /// Configure the app settings to use the provided <paramref name="prefix"/> in the elasticsearch indexes created by
    /// the <see cref="ElasticIndexStep"/>.
    /// </summary>
    public static void ConfigureElasticSearchPrefix(this OrchardCoreConfiguration configuration, string prefix) =>
        configuration.BeforeAppStart += (_, arguments) =>
        {
            arguments.AddWithValue("OrchardCore:OrchardCore_Elasticsearch:IndexPrefix", prefix);
            return Task.CompletedTask;
        };
}
