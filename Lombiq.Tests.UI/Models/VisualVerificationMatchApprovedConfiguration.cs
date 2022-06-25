using Lombiq.Tests.UI.Attributes;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchApprovedConfiguration : VisualVerificationMatchConfiguration<VisualVerificationMatchApprovedConfiguration>
{
    /// <summary>
    /// Gets the callback to provide the reference file name. Parameters: the configuration, the module name,
    /// the method name.
    /// </summary>
    public Func<VisualVerificationMatchApprovedConfiguration, string, string, string> ReferenceFileNameFormatter { get; private set; }
        = (configuration, moduleName, functionName) => new[]
        {
            configuration.FileNamePrefix,
            moduleName,
            functionName,
            configuration.FileNameSuffix,
            configuration.UsePlatformSuffix ? Environment.OSVersion.Platform.ToString() : null,
        }
        .JoinNotNullOrEmpty("_");

    /// <summary>
    /// Gets the stack offset relative to the first method in the call stack which is not decorated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/>.
    /// </summary>
    public int StackOffset { get; private set; }

    /// <summary>
    /// Gets the list of <see cref="PlatformID"/> where the test can be run. If null, all platforms are selected.
    /// </summary>
    public IEnumerable<PlatformID> Platforms { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="PlatformID"/> is used as a suffix of reference file name.
    /// </summary>
    public bool UsePlatformSuffix { get; private set; }

    /// <summary>
    /// Sets <see cref="ReferenceFileNameFormatter"/>.
    /// </summary>
    /// <param name="formatter">
    /// Callback to provide the reference file name. The first parameter is the module name, the second parameter is the
    /// method name.
    /// </param>
    public VisualVerificationMatchApprovedConfiguration WithReferenceFileNameFormatter(
        Func<VisualVerificationMatchApprovedConfiguration, string, string, string> formatter)
    {
        ReferenceFileNameFormatter = formatter;

        return this;
    }

    /// <summary>
    /// Sets <see cref="StackOffset"/> to caller method.
    /// </summary>
    public VisualVerificationMatchApprovedConfiguration WithCallerLocation()
    {
        StackOffset = 1;

        return this;
    }

    /// <summary>
    /// Sets <see cref="Platforms"/>.
    /// </summary>
    public VisualVerificationMatchApprovedConfiguration WithPlatforms(params PlatformID[] platforms)
    {
        Platforms = platforms;

        return this;
    }

    /// <summary>
    /// Sets <see cref="UsePlatformSuffix"/>.
    /// </summary>
    public VisualVerificationMatchApprovedConfiguration WithUsePlatformSuffix()
    {
        UsePlatformSuffix = true;

        return this;
    }
}
