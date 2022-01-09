using Lombiq.Tests.UI.Services;
using System;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Models
{
    public class UITestManifest
    {
        public ITest XunitTest { get; }
        public string Name => XunitTest.DisplayName;
        public Action<UITestContext> Test { get; set; }

        public UITestManifest(ITestOutputHelper testOutputHelper) =>
            XunitTest = testOutputHelper.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(field => field.FieldType == typeof(ITest))
                ?.GetValue(testOutputHelper) as ITest;
    }
}
