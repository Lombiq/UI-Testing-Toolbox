using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class OrchardCoreBuilderExtensions
{
    /// <summary>
    /// Enables the <c>OrchardCore.AutoSetup</c> feature if the <paramref name="configuration"/> doesn't indicate UI
    /// testing.
    /// </summary>
    public static OrchardCoreBuilder EnableAutoSetupIfNotUITesting(
        this OrchardCoreBuilder orchardCoreBuilder,
        IConfiguration configuration) =>
        !configuration.IsUITesting()
            ? orchardCoreBuilder.AddSetupFeatures("OrchardCore.AutoSetup")
            : orchardCoreBuilder;
}
