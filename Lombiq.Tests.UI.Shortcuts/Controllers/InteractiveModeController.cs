using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

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

    [AllowAnonymous]
    [Route("api/InteractiveMode/IsInteractive")]
    public IActionResult IsInteractive() => Json(_interactiveModeStatusAccessor.Enabled);

    public IActionResult Continue()
    {
        _interactiveModeStatusAccessor.Enabled = false;
        return Ok();
    }
}
