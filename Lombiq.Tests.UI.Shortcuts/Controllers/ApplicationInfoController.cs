using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[ApiController]
[Route("api/ApplicationInfo")]
[DevelopmentAndLocalhostOnly]
public class ApplicationInfoController(IApplicationContext applicationContext) : Controller
{
    [HttpGet]
    public IActionResult Get() => Ok(applicationContext.GetApplicationInfo());
}
