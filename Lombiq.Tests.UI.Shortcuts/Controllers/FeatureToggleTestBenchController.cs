using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Feature(ShortcutsFeatureIds.FeatureToggleTestBench)]
public class FeatureToggleTestBenchController : Controller
{
    // While the warning doesn't show up in VS it does with dotnet build.
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "It's a controller action that needs to be instance-level.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    [SuppressMessage(
        "Minor Code Smell",
        "S3400:Methods should not return constants",
        Justification = "Necessary to check that it works when run from a test.")]
    public string Index() => "The Feature Toggle Test Bench worked.";
}
