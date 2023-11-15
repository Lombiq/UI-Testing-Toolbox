using System.IO;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class AutomationFrameworkYamlPaths
{
    private static readonly string AutomationFrameworkYamlsPath = Path.Combine("SecurityScanning", "AutomationFrameworkYamls");

    public static readonly string BaselineYamlPath = Path.Combine(AutomationFrameworkYamlsPath, "Baseline.yml");
    public static readonly string FullScanYamlPath = Path.Combine(AutomationFrameworkYamlsPath, "FullScan.yml");
    public static readonly string GraphQLYamlPath = Path.Combine(AutomationFrameworkYamlsPath, "GraphQL.yml");
    public static readonly string OpenAPIYamlPath = Path.Combine(AutomationFrameworkYamlsPath, "OpenAPI.yml");
}
