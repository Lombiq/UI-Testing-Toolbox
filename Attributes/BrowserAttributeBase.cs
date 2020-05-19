using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class BrowserAttributeBase : DataAttribute
    {
        protected abstract Browser Browser { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new[] { Browser as object };
        }
    }
}
