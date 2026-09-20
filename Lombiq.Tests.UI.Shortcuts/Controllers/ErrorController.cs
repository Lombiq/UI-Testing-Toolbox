using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public sealed class ErrorController : Controller
{
    public const string ExceptionMessage = "This action intentionally causes an exception!";

    private readonly Lazy<TestHealthCheckStatusAccessor> _testHealthCheckStatusAccessorLazy;

    public ErrorController(Lazy<TestHealthCheckStatusAccessor> testHealthCheckStatusAccessorLazy) =>
        _testHealthCheckStatusAccessorLazy = testHealthCheckStatusAccessorLazy;

    [AllowAnonymous]
    public IActionResult Index() => throw new InvalidOperationException(ExceptionMessage);

    [AllowAnonymous]
    public IActionResult HealthCheck(string status, string description)
    {
        var accessor = _testHealthCheckStatusAccessorLazy.Value;

        accessor.Status = Enum.Parse<HealthStatus>(status ?? nameof(HealthStatus.Unhealthy));
        accessor.Description = description ?? nameof(ErrorController);

        return Redirect("~/health/live");
    }
}
