using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.DisplayManagement.Notify;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[AllowAnonymous]
[DevelopmentAndLocalhostOnly]
public sealed class InteractiveModeController : Controller
{
    private readonly IInteractiveModeStatusAccessor _interactiveModeStatusAccessor;
    private readonly INotifier _notifier;

    public InteractiveModeController(
        IInteractiveModeStatusAccessor interactiveModeStatusAccessor,
        INotifier notifier)
    {
        _interactiveModeStatusAccessor = interactiveModeStatusAccessor;
        _notifier = notifier;
    }

    public async Task<IActionResult> Index(string notificationHtml)
    {
        _interactiveModeStatusAccessor.Enabled = true;

        if (!string.IsNullOrWhiteSpace(notificationHtml))
        {
            var message = new LocalizedHtmlString(notificationHtml, notificationHtml);
            await _notifier.InformationAsync(message);
        }

        return View();
    }

    [Route("api/InteractiveMode/IsInteractive")]
    [HttpGet]
    public IActionResult IsInteractive() => Json(_interactiveModeStatusAccessor.Enabled);

    public IActionResult Continue()
    {
        _interactiveModeStatusAccessor.Enabled = false;
        return Ok();
    }
}
