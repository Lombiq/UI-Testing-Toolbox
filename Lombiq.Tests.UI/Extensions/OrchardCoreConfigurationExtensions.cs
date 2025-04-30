using Lombiq.Tests.UI.Services;
using OrchardCore.Search.Elasticsearch.Core.Recipes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Necessary due to a typo.")]
public static class OrchardCoreConfigurationExtensions
{
    // When removing this, also remove the SuppressMessage attribute above.
    [Obsolete("Use ConfigureElasticsearchPrefix instead. This method will be removed in a future version.")]
    public static void ConfigureElasticSearchPrefix(this OrchardCoreConfiguration configuration, string prefix) =>
        ConfigureElasticsearchPrefix(configuration, prefix);

    /// <summary>
    /// Configure the app settings to use the provided <paramref name="prefix"/> in the Elasticsearch indexes created by
    /// the <see cref="ElasticIndexStep"/>.
    /// </summary>
    public static void ConfigureElasticsearchPrefix(this OrchardCoreConfiguration configuration, string prefix) =>
        configuration.BeforeAppStart += (_, arguments) =>
        {
            arguments.AddWithValue("OrchardCore:OrchardCore_Elasticsearch:IndexPrefix", prefix);
            return Task.CompletedTask;
        };
}
