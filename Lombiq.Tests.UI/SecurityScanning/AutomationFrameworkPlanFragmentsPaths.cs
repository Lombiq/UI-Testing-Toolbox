using System.IO;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class AutomationFrameworkPlanFragmentsPaths
{
    private static readonly string AutomationFrameworkPlanFragmentsPath =
        Path.Combine("SecurityScanning", "AutomationFrameworkPlanFragments");

    public static readonly string DisplayActiveScanRuleRuntimesScriptPath =
        Path.Combine(AutomationFrameworkPlanFragmentsPath, "DisplayActiveScanRuleRuntimesScript.yml");
    public static readonly string RequestorJobPath = Path.Combine(AutomationFrameworkPlanFragmentsPath, "RequestorJob.yml");
    public static readonly string SpiderAjaxJobPath = Path.Combine(AutomationFrameworkPlanFragmentsPath, "SpiderAjaxJob.yml");
}
