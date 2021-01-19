using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class ShellFeaturesController : Controller
    {
        private readonly IShellFeaturesManager _shellFeatureManager;
        private readonly IExtensionManager _extensionManager;

        public ShellFeaturesController(IShellFeaturesManager shellFeatureManager, IExtensionManager extensionManager)
        {
            _shellFeatureManager = shellFeatureManager;
            _extensionManager = extensionManager;
        }

        public async Task<IActionResult> EnableFeatureDirectly(string featureId)
        {
            var feature = GetFeature(featureId);

            if (feature == null) return StatusCode(404);

            await _shellFeatureManager.EnableFeaturesAsync(new[] { feature }, true);

            return StatusCode(200);
        }

        public async Task<IActionResult> DisableFeatureDirectly(string featureId)
        {
            var feature = GetFeature(featureId);

            if (feature == null) return StatusCode(404);

            await _shellFeatureManager.DisableFeaturesAsync(new[] { feature }, true);

            return StatusCode(200);
        }

        private IFeatureInfo GetFeature(string featureId) =>
            _extensionManager.GetFeatures().FirstOrDefault(feature => feature.Id.Equals(featureId, StringComparison.Ordinal));
    }
}
