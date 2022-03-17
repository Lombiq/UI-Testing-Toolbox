using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[ApiController]
[Route("api/ApplicationInfo")]
[DevelopmentAndLocalhostOnly]
public class ApplicationInfoController : Controller
{
    private readonly IApplicationContext _applicationContext;

    public ApplicationInfoController(IApplicationContext applicationContext) => _applicationContext = applicationContext;

    [HttpGet]
    public IActionResult Get() => Ok(_applicationContext.GetApplicationInfo());
}
