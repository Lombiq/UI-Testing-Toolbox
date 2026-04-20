#nullable enable

using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Provides data about the currently executing test.
/// </summary>
public class UITestManifest
{
    public ITest? XunitTest => TestContext.Current.Test;
    public string? Name => XunitTest?.TestDisplayName;
    public Func<UITestContext, Task> TestAsync { get; }
    public EnhancedStackFrame? StackFrame { get; }

    public UITestManifest(Func<UITestContext, Task> testAsync)
    {
        TestAsync = testAsync;

        var typeName = testAsync.Method.DeclaringType?.FullName?.Split('+')[0];
        var methodName = testAsync.Method.Name.StartsWith('<') ? testAsync.Method.Name[1..].Split('>')[0] : testAsync.Method.Name;
        StackFrame = new EnhancedStackTrace(new StackTrace(fNeedFileInfo: true))
            .FirstOrDefault(frame => frame.MethodInfo.DeclaringType?.FullName == typeName && frame.MethodInfo.Name == methodName);
    }
}
