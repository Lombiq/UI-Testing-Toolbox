using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using System;

namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchApprovedConfiguration : VisualVerificationMatchConfiguration<VisualVerificationMatchApprovedConfiguration>
{
    /// <summary>
    /// Sets <see cref="ReferenceFileNameFormatter"/>.
    /// </summary>
    /// <param name="formatter">Callback to provide the reference file name. The first parameter is the module name,
    /// the second parameter is the method name.</param>
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
        }
        .JoinNotEmptySafe("_");

    /// <summary>
    /// Gets the stack offset relative to the first method in the call stack which is not decorated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/>.
    /// </summary>
    public int StackOffset { get; private set; }
}
