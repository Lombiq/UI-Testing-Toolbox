using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class ErrorController : Controller
    {
        public const string ExceptionMessage = "This action causes an exception!";

        [AllowAnonymous]
        public IActionResult Index() =>
            throw new InvalidOperationException(ExceptionMessage);
    }
}
