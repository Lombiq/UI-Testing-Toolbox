using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[AllowAnonymous]
[DevelopmentAndLocalhostOnly]
public class InteractiveModeController : Controller
{
    private readonly IInteractiveModeStatusAccessor _interactiveModeStatusAccessor;

    public InteractiveModeController(IInteractiveModeStatusAccessor interactiveModeStatusAccessor) =>
        _interactiveModeStatusAccessor = interactiveModeStatusAccessor;

    public IActionResult Index()
    {
        _interactiveModeStatusAccessor.Enabled = true;
        return View();
    }

    [Route("api/InteractiveMode/IsInteractive")]
    public IActionResult IsInteractive() => Json(_interactiveModeStatusAccessor.Enabled);

    public IActionResult Continue()
    {
        _interactiveModeStatusAccessor.Enabled = false;
        return Ok();
    }
}
