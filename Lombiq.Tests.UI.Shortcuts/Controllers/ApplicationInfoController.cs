using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[ApiController]
[Route("api/ApplicationInfo")]
[DevelopmentAndLocalhostOnly]
public sealed class ApplicationInfoController : ControllerBase
{
    private readonly IApplicationContext _applicationContext;

    public ApplicationInfoController(IApplicationContext applicationContext) => _applicationContext = applicationContext;

    [HttpGet]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(_applicationContext.GetApplicationInfo());
}
