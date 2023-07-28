using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

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

    public IActionResult Status() => Json(_interactiveModeStatusAccessor);

    public IActionResult Continue()
    {
        _interactiveModeStatusAccessor.Enabled = false;
        return Ok();
    }
}
