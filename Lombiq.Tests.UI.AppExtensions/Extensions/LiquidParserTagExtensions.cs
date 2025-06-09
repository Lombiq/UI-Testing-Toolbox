using Lombiq.Tests.UI.AppExtensions.ParserTags;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class LiquidParserTagExtensions
{
    /// <summary>
    /// Adds the <see cref="IsUITestingLiquidParserTag"/> liquid parser tag as "is_ui_testing".
    /// </summary>
    public static OrchardCoreBuilder AddIsUITestingLiquidParserTag(
        this OrchardCoreBuilder orchardCoreBuilder,
        IConfiguration configuration) =>
        orchardCoreBuilder.ConfigureServices((services) =>
            services.AddLiquidEmptyTag<IsUITestingLiquidParserTag>("is_ui_testing"));
}
