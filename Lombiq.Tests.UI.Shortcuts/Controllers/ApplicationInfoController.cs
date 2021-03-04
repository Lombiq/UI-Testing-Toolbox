using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Lombiq.Tests.UI.Shortcuts.Models;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;
using System.Linq;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [ApiController]
    [Route("api/ApplicationInfo")]
    [DevelopmentAndLocalhostOnly]
    public class ApplicationInfoController : Controller
    {
        private readonly IApplicationContext _applicationContext;

        public ApplicationInfoController(IApplicationContext applicationContext) => _applicationContext = applicationContext;

        [HttpGet]
        public IActionResult Get()
        {
            var application = _applicationContext.Application;
            return Ok(new ApplicationInfo
            {
                AppRoot = application.Root,
                AssemblyInfo = new AssemblyInfo
                {
                    AssemblyLocation = application.Assembly.Location,
                    AssemblyName = application.Assembly.ToString(),
                },
                Modules = application.Modules.Select(
                    module => new AssemblyInfo
                    {
                        AssemblyLocation = module.Assembly.Location,
                        AssemblyName = module.Assembly.ToString(),
                    }),
            });
        }
    }
}
