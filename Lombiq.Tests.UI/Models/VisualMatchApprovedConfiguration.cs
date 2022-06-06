using System;

namespace Lombiq.Tests.UI.Models;

public class VisualMatchApprovedConfiguration : VisualMatchConfiguration<VisualMatchApprovedConfiguration>
{
    /// <summary>
    /// Sets <see cref="ReferenceFileNameFormatter"/>.
    /// </summary>
    /// <param name="formatter">Callback to provide the reference file name. The first parameter is the module name,
    /// the second parameter is the method name.</param>
    public VisualMatchApprovedConfiguration WithReferenceFileNameFormatter(Func<string, string, string> formatter)
    {
        ReferenceFileNameFormatter = formatter;

        return this;
    }

    /// <summary>
    /// Gets the callback to provide the reference file name. The first parameter is the module name,
    /// the second parameter is the method name.
    /// </summary>
    public Func<string, string, string> ReferenceFileNameFormatter { get; private set; } = (moduleName, functionName) =>
        $"{moduleName}_{functionName}";
}
