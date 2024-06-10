using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[AllowAnonymous]
[DevelopmentAndLocalhostOnly]
[SuppressMessage(
    "Major Code Smell",
    "S6934:A Route attribute should be added to the controller when a route template is specified at the action level",
    Justification = "Using attribute routing on the Controller breaks the behavior and will result in a 404." +
    "[controller]/{action=Index} on the controller will not hit the Index action method.")]
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
