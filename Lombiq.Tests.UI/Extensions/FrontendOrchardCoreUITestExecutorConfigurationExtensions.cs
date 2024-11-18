using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class FrontendOrchardCoreUITestExecutorConfigurationExtensions
{
    private const string BackendUri = nameof(BackendUri);
    private const string FrontendUri = nameof(FrontendUri);

    /// <summary>
    /// Returns the start URLs for the frontend and the Orchard Core backend from the <see
    /// cref="OrchardCoreUITestExecutorConfiguration.CustomConfiguration"/>.
    /// </summary>
    public static (Uri FrontendUri, Uri BackendUri) GetFrontendAndBackendUris(
        this OrchardCoreUITestExecutorConfiguration configuration) =>
    (
        configuration.CustomConfiguration.GetMaybe(FrontendUri) as Uri,
        configuration.CustomConfiguration.GetMaybe(BackendUri) as Uri
    );

    /// <summary>
    /// Updates the <see cref="OrchardCoreUITestExecutorConfiguration.CustomConfiguration"/> by storing the frontend and
    /// the Orchard Core backend URLs as <see cref="Uri"/> instances. If either parameter is <see langword="null"/>,
    /// that value is not changed.
    /// </summary>
    public static void SetFrontendAndBackendUris(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string frontendUrl,
        string backendUrl)
    {
        if (frontendUrl != null) configuration.CustomConfiguration[FrontendUri] = new Uri(frontendUrl);
        if (backendUrl != null) configuration.CustomConfiguration[BackendUri] = new Uri(backendUrl);
    }
}
