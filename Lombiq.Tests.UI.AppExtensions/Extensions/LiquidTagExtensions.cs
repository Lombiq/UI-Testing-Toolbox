using Lombiq.Tests.UI.AppExtensions.LiquidTags;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class LiquidTagExtensions
{
    /// <summary>
    /// Adds the <see cref="IsUITestingLiquidEmptyTag"/> property registrar.
    /// </summary>
    public static OrchardCoreBuilder AddIsUITestingLiquidEmptyTag(
        this OrchardCoreBuilder orchardCoreBuilder,
        IConfiguration configuration) =>
        orchardCoreBuilder.ConfigureServices((services) =>
            services.AddLiquidEmptyTag<IsUITestingLiquidEmptyTag>("is_ui_testing"));
}
