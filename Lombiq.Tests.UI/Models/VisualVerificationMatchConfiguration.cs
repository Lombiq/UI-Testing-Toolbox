using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchConfiguration<TSelf>
    where TSelf : VisualVerificationMatchConfiguration<TSelf>
{
    /// <summary>
    /// Gets the prefix applied to all file names.
    /// </summary>
    public string FileNamePrefix { get; private set; }

    /// <summary>
    /// Gets the suffix applied to all file names.
    /// </summary>
    public string FileNameSuffix { get; private set; }

    /// <summary>
    /// Gets the list of <see cref="PlatformID"/>s where the test can be run. If null, all platforms are selected.
    /// </summary>
    public IEnumerable<PlatformID> Platforms { get; private set; }

    /// <summary>
    /// Sets <see cref="FileNamePrefix"/>.
    /// </summary>
    /// <param name="value">The prefix applied to all file names.</param>
    public TSelf WithFileNamePrefix(string value)
    {
        FileNamePrefix = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="FileNameSuffix"/>.
    /// </summary>
    /// <param name="value">The suffix applied to all file names.</param>
    public TSelf WithFileNameSuffix(string value)
    {
        FileNameSuffix = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="Platforms"/>.
    /// </summary>
    public TSelf WithPlatforms(params PlatformID[] platforms)
    {
        Platforms = platforms;

        return (TSelf)this;
    }
}

public class VisualMatchConfiguration : VisualVerificationMatchConfiguration<VisualMatchConfiguration> { }
