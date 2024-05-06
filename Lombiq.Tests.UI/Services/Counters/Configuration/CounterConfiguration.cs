using System;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class CounterConfiguration
{
    /// <summary>
    /// Gets or sets the counter assertion method.
    /// </summary>
    public Action<ICounterDataCollector, ICounterProbe> AssertCounterData { get; set; }

    /// <summary>
    /// Gets or sets the exclude filter. Can be used to exclude counted values before assertion.
    /// </summary>
    public Func<ICounterKey, bool> ExcludeFilter { get; set; }

    /// <summary>
    /// Gets or sets threshold configuration used under navigation requests. See:
    /// <see cref="UI.Extensions.NavigationUITestContextExtensions.GoToAbsoluteUrlAsync(UITestContext, Uri, bool)"/>.
    /// See: <see cref="NavigationProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration NavigationThreshold { get; set; } = new();

    /// <summary>
    /// Gets or sets threshold configuration used per <see cref="YesSql.ISession"/> lifetime. See:
    /// <see cref="SessionProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration SessionThreshold { get; set; } = new();

    /// <summary>
    /// Gets or sets threshold configuration used per page load. See: <see cref="PageLoadProbe"/>.
    /// </summary>
    public CounterThresholdConfiguration PageLoadThreshold { get; set; } = new();
}
