using Microsoft.AspNetCore.Mvc;
using OrchardCore.Media;
using OrchardCore.Modules;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [Feature(ShortcutsFeatureIds.AzureCachePurgeController)]
    public class AzureCachePurgeController : Controller
    {
        private readonly IMediaFileStoreCache _mediaFileStoreCache;

        public AzureCachePurgeController(IMediaFileStoreCache mediaFileStoreCache)
            => _mediaFileStoreCache = mediaFileStoreCache;

        public async Task<IActionResult> PurgeAzureCacheDirectly()
        {
            var hasErrors = await _mediaFileStoreCache.PurgeAsync();

            return hasErrors ? NotFound() : Ok();
        }
    }
}
