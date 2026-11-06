#nullable enable

using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Provides data about the currently executing test.
/// </summary>
public class UITestManifest
{
    public ITest? XunitTest => TestContext.Current.Test;
    public string? Name => XunitTest?.TestDisplayName;

    [Obsolete($"Access the first item of {nameof(Tests)} directly.")]
    public Func<UITestContext, Task> TestAsync => Tests.Single();
    public IList<Func<UITestContext, Task>> Tests { get; } = [];

    public EnhancedStackFrame? StackFrame { get; }

    public UITestManifest(params IEnumerable<Func<UITestContext, Task>> tests)
    {
        Tests.AddRange(tests);
        StackFrame = new EnhancedStackTrace(new StackTrace(fNeedFileInfo: true))
            .First(frame => frame
                .MethodInfo
                .MethodBase?
                .GetCustomAttributes(inherit: true)
                .Any(attribute => attribute is IFactAttribute or ITheoryAttribute) == true);
    }
}
