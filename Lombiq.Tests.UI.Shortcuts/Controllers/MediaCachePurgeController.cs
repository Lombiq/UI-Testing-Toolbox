using Microsoft.AspNetCore.Mvc;
using OrchardCore.Media;
using OrchardCore.Modules;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Feature(ShortcutsFeatureIds.MediaCachePurge)]
public class MediaCachePurgeController(IMediaFileStoreCache mediaFileStoreCache) : Controller
{
    public async Task<IActionResult> PurgeMediaCacheDirectly()
    {
        var hasErrors = await mediaFileStoreCache.PurgeAsync();

        return hasErrors ? StatusCode(500) : Ok();
    }
}
