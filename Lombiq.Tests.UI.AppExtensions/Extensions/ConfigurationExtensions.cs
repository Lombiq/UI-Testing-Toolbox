namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static bool IsUITesting(this IConfiguration configuration) =>
        configuration.GetValue("Lombiq_Tests_UI:IsUITesting", defaultValue: false);
}
