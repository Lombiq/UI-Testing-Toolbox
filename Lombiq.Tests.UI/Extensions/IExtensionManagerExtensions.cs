using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using System;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class IExtensionManagerExtensions
{
    /// <summary>
    /// Gets <see cref="IFeatureInfo"/> by <paramref name="featureId"/>.
    /// </summary>
    /// <returns><see cref="IFeatureInfo"/> instance or null if not exists.</returns>
    public static IFeatureInfo GetFeature(this IExtensionManager extensionManager, string featureId) =>
        extensionManager.GetFeatures().FirstOrDefault(feature => feature.Id.Equals(featureId, StringComparison.Ordinal));
}
