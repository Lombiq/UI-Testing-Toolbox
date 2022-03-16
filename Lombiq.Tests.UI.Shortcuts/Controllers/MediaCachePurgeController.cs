using Microsoft.AspNetCore.Mvc;
using OrchardCore.Media;
using OrchardCore.Modules;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Feature(ShortcutsFeatureIds.MediaCachePurge)]
public class MediaCachePurgeController : Controller
{
    private readonly IMediaFileStoreCache _mediaFileStoreCache;

    public MediaCachePurgeController(IMediaFileStoreCache mediaFileStoreCache)
        => _mediaFileStoreCache = mediaFileStoreCache;

    public async Task<IActionResult> PurgeMediaCacheDirectly()
    {
        var hasErrors = await _mediaFileStoreCache.PurgeAsync();

        return hasErrors ? StatusCode(500) : Ok();
    }
}
