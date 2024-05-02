using System;

namespace Lombiq.Tests.UI.Services;

public class TimeoutConfiguration
{
    /// <summary>
    /// Gets or sets how long to wait for an operation to finish when it's retried. Defaults to 10s. Higher,
    /// paradoxically, is usually less safe.
    /// </summary>
    public TimeSpan RetryTimeout { get; set; }

    /// <summary>
    /// Gets or sets how long to wait between retries after the operation didn't complete successfully. Note that this
    /// is included in <see cref="RetryTimeout"/>. Defaults to 500ms.
    /// </summary>
    /// <example>
    /// Something permanently failing will be checked for a total of 10s with a <see cref="RetryTimeout"/> of 10s and
    /// <see cref="RetryInterval"/> of 5s, just then there will be two-three checks altogether (first check fails, wait
    /// 5s, second check fails, wait 5s, and if we're within 10s still, a third check will also fail).
    /// </example>
    public TimeSpan RetryInterval { get; set; }

    /// <summary>
    /// Gets or sets how long to wait for a page load to finish. Note that this is mostly for the initial page load of
    /// an app (i.e. app start), running the Orchard setup and importing large recipes. Defaults to 180s.
    /// </summary>
    public TimeSpan PageLoadTimeout { get; set; }

    /// <summary>
    /// Gets or sets how long an individual test can run to prevent hanging indefinitely. Defaults to 600s.
    /// </summary>
    public TimeSpan TestRunTimeout { get; set; }

    public static readonly TimeoutConfiguration Default = new()
    {
        RetryTimeout = GetTimeoutConfiguration(nameof(RetryTimeout), 10),
        RetryInterval = GetTimeoutConfiguration(nameof(RetryInterval), 500, useMilliseconds: true),
        PageLoadTimeout = GetTimeoutConfiguration(nameof(PageLoadTimeout), 180),
        TestRunTimeout = GetTimeoutConfiguration(nameof(TestRunTimeout), 600),
    };

    private static TimeSpan GetTimeoutConfiguration(string name, int defaultValue, bool useMilliseconds = false)
    {
        var suffix = useMilliseconds ? "Milliseconds" : "Seconds";
        var key = $"{nameof(TimeoutConfiguration)}:{name}{suffix}";
        var value = TestConfigurationManager.GetIntConfiguration(key, defaultValue);
        return useMilliseconds ? TimeSpan.FromMilliseconds(value) : TimeSpan.FromSeconds(value);
    }
}
