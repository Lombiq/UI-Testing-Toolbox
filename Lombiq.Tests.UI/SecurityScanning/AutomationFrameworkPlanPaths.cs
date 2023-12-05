using System.IO;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class AutomationFrameworkPlanPaths
{
    private static readonly string AutomationFrameworkPlansPath = Path.Combine("SecurityScanning", "AutomationFrameworkPlans");

    public static readonly string BaselinePlanPath = Path.Combine(AutomationFrameworkPlansPath, "Baseline.yml");
    public static readonly string FullScanPlanPath = Path.Combine(AutomationFrameworkPlansPath, "FullScan.yml");
    public static readonly string GraphQLPlanPath = Path.Combine(AutomationFrameworkPlansPath, "GraphQL.yml");
    public static readonly string OpenAPIPlanPath = Path.Combine(AutomationFrameworkPlansPath, "OpenAPI.yml");
}
