using Lombiq.Tests.UI.Services;
using System;
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
    public string Name => XunitTest?.DisplayName;
    public Func<UITestContext, Task> TestAsync { get; set; }

    public UITestManifest(ITestOutputHelper testOutputHelper)
    {
        var original = testOutputHelper;

        do
        {
            XunitTest = GetValueOfType<ITest>(testOutputHelper);
            if (XunitTest == null) testOutputHelper = GetValueOfType<ITestOutputHelper>(testOutputHelper);
        }
        while (XunitTest == null && testOutputHelper != null);

        if (XunitTest == null)
        {
            throw new InvalidOperationException($"Unable to acquire the {original.GetType()}'s unit test.");
        }
    }

    private static T GetValueOfType<T>(object instance)
        where T : class =>
        instance
            .GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Find(field => field.FieldType == typeof(T))?.GetValue(instance) as T;
}
