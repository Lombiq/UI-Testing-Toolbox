using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
public class TimeShiftController : Controller
{
    private readonly IClock _clock;

    public TimeShiftController(IClock clock) => _clock = clock;

    public IActionResult Set(double days, double seconds) =>
        SetInner(_ => TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    public IActionResult Add(double days, double seconds) =>
        SetInner(current => current + TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    private IActionResult SetInner(Func<TimeSpan, TimeSpan> edit) =>
        TimeShifterClock.UpdateClock(_clock, edit) is { } totalSeconds
            ? Ok(totalSeconds)
            : BadRequest($"The clock is {_clock.GetType().FullName} instead of {nameof(TimeShifterClock)}.");
}
