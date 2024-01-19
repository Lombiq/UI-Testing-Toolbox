using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class ErrorController : Controller
{
    public const string ExceptionMessage = "This action intentionally causes an exception!";

    // This only happens in the CI for some reason.
    [AllowAnonymous]
    public IActionResult Index() =>
        throw new InvalidOperationException(ExceptionMessage);
}
