using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class ErrorController : Controller
    {
        public const string ExceptionMessage = "This action causes an exception!";

        // This only happens in the CI for some reason.
        [SuppressMessage("Usage", "CA1822:Mark members as static", Justification = "It's a controller action.")]
        [AllowAnonymous]
        public IActionResult Index() =>
            throw new InvalidOperationException(ExceptionMessage);
    }
}
