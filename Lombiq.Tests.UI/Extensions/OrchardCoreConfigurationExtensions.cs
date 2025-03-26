using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class OrchardCoreConfigurationExtensions
{
    /// <summary>
    /// Configure the app settings to use the provided <paramref name="prefix"/> in the elasticsearch indexes created by
    /// the <see cref="TestPrefixedElasticsearchIndexStep"/>.
    /// </summary>
    public static void ConfigureElasticSearchPrefix(this OrchardCoreConfiguration configuration, string prefix) =>
        configuration.BeforeAppStart += (_, arguments) =>
        {
            arguments.AddWithValue($"OrchardCore:{TestPrefixedElasticsearchIndexStep.ConfigurationKey}", prefix);
            return Task.CompletedTask;
        };
}
