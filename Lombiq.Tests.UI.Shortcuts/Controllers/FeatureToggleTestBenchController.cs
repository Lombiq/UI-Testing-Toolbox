using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Feature(ShortcutsFeatureIds.FeatureToggleTestBench)]
public sealed class FeatureToggleTestBenchController : Controller
{
    public IActionResult Index() => Content("The Feature Toggle Test Bench worked.");
}
