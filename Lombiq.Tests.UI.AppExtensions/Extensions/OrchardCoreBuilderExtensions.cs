using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class OrchardCoreBuilderExtensions
{
    public static OrchardCoreBuilder EnableAutoSetupIfNotUITesting(
        this OrchardCoreBuilder orchardCoreBuilder,
        IConfiguration configuration) =>
        !configuration.IsUITesting()
            ? orchardCoreBuilder.AddSetupFeatures("OrchardCore.AutoSetup")
            : orchardCoreBuilder;
}
