using Lombiq.Tests.UI.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Provides data about the currently executing test.
/// </summary>
public class UITestManifest
{
    public ITest XunitTest { get; }
    public string Name => XunitTest.DisplayName;
    public Func<UITestContext, Task> TestAsync { get; set; }

    public UITestManifest(ITestOutputHelper testOutputHelper) =>
        XunitTest = testOutputHelper.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(field => field.FieldType == typeof(ITest))
            ?.GetValue(testOutputHelper) as ITest;
}
