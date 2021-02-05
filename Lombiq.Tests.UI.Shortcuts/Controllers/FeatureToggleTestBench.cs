using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [Feature(ShortcutsFeatureIds.FeatureToggleTestBench)]
    public class FeatureToggleTestBench : Controller
    {
        public string Index() => "The Feature Toggle Test Bench worked.";
    }
}
