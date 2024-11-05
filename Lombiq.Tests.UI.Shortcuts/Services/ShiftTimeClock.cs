using OrchardCore.Modules;
using System;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class ShiftTimeClock : IClock
{
    private readonly Clock _inner = new();

    public TimeSpan Shift { get; set; }
    public DateTime UtcNow => _inner.UtcNow + Shift;

    public ITimeZone[] GetTimeZones() => _inner.GetTimeZones();

    public ITimeZone GetTimeZone(string timeZoneId) => _inner.GetTimeZone(timeZoneId);

    public ITimeZone GetSystemTimeZone() => _inner.GetSystemTimeZone();

    public DateTimeOffset ConvertToTimeZone(DateTimeOffset dateTimeOffset, ITimeZone timeZone) =>
        _inner.ConvertToTimeZone(dateTimeOffset, timeZone);
}
