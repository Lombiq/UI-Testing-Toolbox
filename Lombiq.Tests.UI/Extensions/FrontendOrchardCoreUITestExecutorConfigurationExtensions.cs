using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Extensions;

public static class FrontendOrchardCoreUITestExecutorConfigurationExtensions
{
    private const string BackendUri = nameof(BackendUri);
    private const string FrontendUri = nameof(FrontendUri);

    /// <summary>
    /// Returns the start URLs for the Vite frontend and the Orchard Core backend from the <see
    /// cref="OrchardCoreUITestExecutorConfiguration.CustomConfiguration"/>.
    /// </summary>
    public static (Uri FrontendUri, Uri BackendUri) GetFrontendAndBackendUris(
        this OrchardCoreUITestExecutorConfiguration configuration) =>
    (
        (Uri)configuration.CustomConfiguration[FrontendUri],
        (Uri)configuration.CustomConfiguration[BackendUri]
    );

    /// <summary>
    /// Updates the <see cref="OrchardCoreUITestExecutorConfiguration.CustomConfiguration"/> by storing the Vite
    /// frontend and the Orchard Core backend URLs as <see cref="Uri"/> instances.
    /// </summary>
    public static void SetFrontendAndBackendUris(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string frontendUrl,
        string backendUrl)
    {
        configuration.CustomConfiguration[FrontendUri] = new Uri(frontendUrl);
        configuration.CustomConfiguration[BackendUri] = new Uri(backendUrl);
    }
}
