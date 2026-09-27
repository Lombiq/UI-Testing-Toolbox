using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public sealed class ErrorController : Controller
{
    public const string ExceptionMessage = "This action intentionally causes an exception!";

    [AllowAnonymous]
    public IActionResult Index() => throw new InvalidOperationException(ExceptionMessage);

    [AllowAnonymous]
    public IActionResult HealthCheck(string status, string description)
    {
        // This service depends on the "OrchardCore.HealthChecks" module, so if it's not registered then we can act like
        // this action doesn't exist.
        if (HttpContext.RequestServices.GetService<TestHealthCheckStatusAccessor>() is not { } accessor)
        {
            return NotFound();
        }

        accessor.Status = Enum.Parse<HealthStatus>(status ?? nameof(HealthStatus.Unhealthy));
        accessor.Description = description ?? nameof(ErrorController);

        return Redirect("~/health/live");
    }
}
