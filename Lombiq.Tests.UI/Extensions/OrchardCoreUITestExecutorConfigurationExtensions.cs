namespace Lombiq.Tests.UI.Services;

public static class OrchardCoreUITestExecutorConfigurationExtensions
{
    /// <summary>
    /// Sets the <see cref="OrchardCoreUITestExecutorConfiguration.AssertAppLogsAsync"/> to the output of <see
    /// cref="OrchardCoreUITestExecutorConfiguration.UseAssertAppLogsForSecurityScan"/> so it accepts security scanning
    /// related errors.
    /// </summary>
    public static OrchardCoreUITestExecutorConfiguration UseAssertAppLogsForSecurityScan(
        this OrchardCoreUITestExecutorConfiguration configuration,
        params string[] additionalPermittedErrorLines)
    {
        configuration.AssertAppLogsAsync = OrchardCoreUITestExecutorConfiguration
            .UseAssertAppLogsForSecurityScan(additionalPermittedErrorLines);

        return configuration;
    }
}
