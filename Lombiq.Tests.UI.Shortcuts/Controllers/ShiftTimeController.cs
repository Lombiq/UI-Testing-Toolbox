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
public class ShiftTimeController : Controller
{
    private readonly IClock _clock;

    public ShiftTimeController(IClock clock) => _clock = clock;

    public IActionResult Set(double days, double seconds) =>
        SetInner(_ => TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    public IActionResult Add(double days, double seconds) =>
        SetInner(current => current + TimeSpan.FromDays(days) + TimeSpan.FromSeconds(seconds));

    private IActionResult SetInner(Func<TimeSpan, TimeSpan> edit)
    {
        if (_clock is not ShiftTimeClock clock) return BadRequest();

        clock.Shift = edit(clock.Shift);
        return Ok((long)clock.Shift.TotalSeconds);
    }
}
