using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions;

public static class UITestBaseExtensions
{
    /// <summary>
    /// Sets the max retry count to zero.
    /// </summary>
    public static UITestBase WithoutRetries(this UITestBase uiTestBase) =>
        uiTestBase.AddConfiguration(configuration => configuration.MaxRetryCount = 0);

    /// <summary>
    /// Sets <see cref="OrchardCoreUITestExecutorConfiguration.UseSmtpService"/> to <see langword="true"/>.
    /// </summary>
    public static UITestBase UseSmtpService(this UITestBase uiTestBase) =>
        uiTestBase.AddConfiguration(configuration => configuration.UseSmtpService = true);
}
