using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Services;

/// <summary>
/// An alternative <see cref="IClock"/> implementation that replaces and wraps <see cref="Clock"/>. Out of the box it
/// behaves exactly the same, the only difference is that <see cref="UtcNow"/> returns <see cref="Clock.UtcNow"/> and
/// <see cref="Shift"/> added together. If you edit that property, you can trick all services that use <see
/// cref="IClock"/> into believing it's the future or past. This service is registered as singleton, so setting <see
/// cref="Shift"/> will persist in the tenant across requests.
/// </summary>
public class TimeShifterClock : IClock
{
    private readonly Clock _inner = new();

    /// <summary>
    /// Gets or sets the offset which is added to <see cref="UtcNow"/>.
    /// </summary>
    public TimeSpan Shift { get; set; } = TimeSpan.Zero;

    public DateTime UtcNow => _inner.UtcNow + Shift;

    public ITimeZone[] GetTimeZones() => _inner.GetTimeZones();

    public ITimeZone GetTimeZone(string timeZoneId) => _inner.GetTimeZone(timeZoneId);

    public ITimeZone GetSystemTimeZone() => _inner.GetSystemTimeZone();

    public DateTimeOffset ConvertToTimeZone(DateTimeOffset dateTimeOffset, ITimeZone timeZone) =>
        _inner.ConvertToTimeZone(dateTimeOffset, timeZone);

    /// <summary>
    /// Use this to safely update the <see cref="Shift"/> value.
    /// </summary>
    /// <param name="clock">
    /// The injected service, if it's <see cref="TimeShifterClock"/> then its <see cref="Shift"/> property is updated.
    /// </param>
    /// <param name="edit">Input is the current value of <see cref="Shift"/>, output is the new value.</param>
    /// <returns>
    /// The total seconds of the new value of <see cref="Shift"/>, or <see langword="null"/> if <paramref name="clock"/>
    /// is not <see cref="TimeShifterClock"/>.
    /// </returns>
    public static double? UpdateClock(IClock clock, Func<TimeSpan, TimeSpan> edit)
    {
        if (clock is not TimeShifterClock shiftTimeClock) return null;

        shiftTimeClock.Shift = edit(shiftTimeClock.Shift);
        return shiftTimeClock.Shift.TotalSeconds;
    }
}
