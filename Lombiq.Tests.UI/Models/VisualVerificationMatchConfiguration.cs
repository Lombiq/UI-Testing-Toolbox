namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchConfiguration<TSelf>
    where TSelf : VisualVerificationMatchConfiguration<TSelf>
{
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
    /// Gets the prefix applied to all file names.
    /// </summary>
    public string FileNamePrefix { get; private set; }

    /// <summary>
    /// Gets the suffix applied to all file names.
    /// </summary>
    public string FileNameSuffix { get; private set; }
}

public class VisualMatchConfiguration : VisualVerificationMatchConfiguration<VisualMatchConfiguration> { }
