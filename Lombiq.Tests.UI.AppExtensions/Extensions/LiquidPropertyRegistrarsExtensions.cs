using Lombiq.Tests.UI.AppExtensions.PropertyRegistrars;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class LiquidPropertyRegistrarsExtensions
{
    /// <summary>
    /// Adds the <see cref="IsUITestingLiquidPropertyRegistrar"/> property registrar.
    /// </summary>
    public static OrchardCoreBuilder AddIsUITestingLiquidPropertyRegistrar(
        this OrchardCoreBuilder orchardCoreBuilder,
        IConfiguration configuration) =>
        orchardCoreBuilder.ConfigureServices((services) =>
            services.RegisterLiquidPropertyAccessor<IsUITestingLiquidPropertyRegistrar>("is_ui_testing"));
}
