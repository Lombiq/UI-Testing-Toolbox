using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AllBrowsersAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var browsers = (IEnumerable<Browser>)Enum.GetValues(typeof(Browser));
            foreach (var browser in browsers)
            {
                yield return new[] { browser as object };
            }
        }
    }
}
