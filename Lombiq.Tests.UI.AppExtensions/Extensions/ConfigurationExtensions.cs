namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets a value indicating whether the application is currently running in a UI test.
    /// </summary>
    public static bool IsUITesting(this IConfiguration configuration) =>
        configuration.GetValue("Lombiq_Tests_UI:IsUITesting", defaultValue: false);

    /// <summary>
    /// Disables the disabling of CDN usage for static resource. By default, CDN usage is disabled during UI testing,
    /// since tests should be possible to run in isolation. You can use this to opt out of this behavior.
    /// </summary>
    public static void DontDisableUseCdn(this IConfiguration configuration) =>
        configuration["Lombiq_Tests_UI:DontDisableUseCdn"] = "true";
}
