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

    public UITestBase Fixture { get; }

    [Obsolete($"Access the {nameof(Tests)} property directly.")]
    public Func<UITestContext, Task> TestAsync => Tests.Single();
    public IReadOnlyList<Func<UITestContext, Task>> Tests { get; }

    public EnhancedStackFrame? StackFrame { get; }

    public UITestManifest(UITestBase fixture, params IReadOnlyList<Func<UITestContext, Task>> tests)
    {
        var stackTrace = new EnhancedStackTrace(new StackTrace(fNeedFileInfo: true));
        Fixture = fixture;
        Tests = tests;
        StackFrame = stackTrace.FirstOrDefault(frame => frame
            .MethodInfo
            .MethodBase?
            .GetCustomAttributes(inherit: true)
            .Any(attribute => attribute is IFactAttribute or ITheoryAttribute) == true);

        if (StackFrame == null)
        {
            var testAsync = tests[^1];
            var typeName = testAsync.Method.DeclaringType?.FullName?.Split('+')[0];
            var methodName = testAsync.Method.Name.StartsWith('<')
                ? testAsync.Method.Name[1..].Split('>')[0]
                : testAsync.Method.Name;

            StackFrame = stackTrace.FirstOrDefault(frame =>
                frame.MethodInfo.DeclaringType?.FullName == typeName && frame.MethodInfo.Name == methodName);
        }
    }

    public async Task RunTestsAsync(UITestContext context)
    {
        foreach (var test in Tests)
        {
            context.Configuration.TestCancellationToken.ThrowIfCancellationRequested();
            await test(context);
        }
    }
}
