using Lombiq.Tests.UI.Attributes;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchApprovedConfiguration :
    VisualVerificationMatchConfiguration<VisualVerificationMatchApprovedConfiguration>
{
    /// <summary>
    /// Gets the callback to provide the baseline file name. Parameters:
    /// <see cref="VisualVerificationMatchApprovedConfiguration"/>, <see cref="VisualVerificationMatchApprovedContext"/>.
    /// </summary>
    public Func<
        VisualVerificationMatchApprovedConfiguration,
        VisualVerificationMatchApprovedContext,
        string> BaselineFileNameFormatter
    { get; private set; } = (configuration, context) => new[]
        {
            configuration.FileNamePrefix,
            context.ModuleName,
            context.MethodName,
            configuration.FileNameSuffix,
            configuration.UsePlatformAsSuffix ? Environment.OSVersion.Platform.ToString() : null,
            configuration.UseBrowserNameAsSuffix ? context.BrowserName : null,
        }
        .JoinNotNullOrEmpty("_");

    /// <summary>
    /// Gets the stack offset relative to the first method in the call stack which is not annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/>.
    /// </summary>
    public int StackOffset { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="PlatformID"/> is used as a suffix of the baseline file name.
    /// </summary>
    public bool UsePlatformAsSuffix { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current browser name is used as a suffix of the baseline file name.
    /// </summary>
    public bool UseBrowserNameAsSuffix { get; private set; }

    /// <summary>
    /// Sets <see cref="BaselineFileNameFormatter"/>.
    /// </summary>
    /// <param name="formatter">
    /// Callback to provide the baseline file name. Parameters:
    /// <see cref="VisualVerificationMatchApprovedConfiguration"/>, <see cref="VisualVerificationMatchApprovedContext"/>.
    /// </param>
    public VisualVerificationMatchApprovedConfiguration WithBaselineFileNameFormatter(
        Func<VisualVerificationMatchApprovedConfiguration, VisualVerificationMatchApprovedContext, string> formatter)
    {
        BaselineFileNameFormatter = formatter;

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
    /// Sets <see cref="UsePlatformAsSuffix"/>.
    /// </summary>
    public VisualVerificationMatchApprovedConfiguration WithUsePlatformAsSuffix()
    {
        UsePlatformAsSuffix = true;

        return this;
    }

    /// <summary>
    /// Sets <see cref="UseBrowserNameAsSuffix"/>.
    /// </summary>
    public VisualVerificationMatchApprovedConfiguration WithUseBrowserNameAsSuffix()
    {
        UseBrowserNameAsSuffix = true;

        return this;
    }
}
