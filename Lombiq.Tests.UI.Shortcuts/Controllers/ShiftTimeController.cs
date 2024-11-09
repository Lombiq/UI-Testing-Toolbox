using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[SuppressMessage(
    "Major Code Smell",
    "S6967:ModelState.IsValid should be called in controller actions",
    Justification = "Not relevant in a test-only controller.")]
[DevelopmentAndLocalhostOnly]
public class ShiftTimeController : Controller
{
    private readonly IClock _clock;

    public ShiftTimeController(IClock clock) => _clock = clock;

    public IActionResult Set(double days, double seconds) =>
        SetInner(_ => TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    public IActionResult Add(double days, double seconds) =>
        SetInner(current => current + TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    private IActionResult SetInner(Func<TimeSpan, TimeSpan> edit) =>
        ShiftTimeClock.UpdateClock(_clock, edit) is { } totalSeconds
            ? Ok(totalSeconds)
            : BadRequest($"The clock is {_clock.GetType().FullName} instead of {nameof(ShiftTimeClock)}.");
}
