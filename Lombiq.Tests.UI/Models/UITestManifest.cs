using Lombiq.Tests.UI.Services;
using System;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Models
{
    /// <summary>
    /// Provides data about the currently executing test.
    /// </summary>
    public class UITestManifest
    {
        public ITestOutputHelper TestOutputHelper { get; }
        public ITest XunitTest { get; }
        public string Name => XunitTest.DisplayName;
        public Action<UITestContext> Test { get; set; }

        public UITestManifest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;

            XunitTest = testOutputHelper.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(field => field.FieldType == typeof(ITest))
                ?.GetValue(testOutputHelper) as ITest;
        }
    }
}
