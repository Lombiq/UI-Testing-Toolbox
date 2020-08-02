using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    [SuppressMessage(
        "Minor Code Smell",
        "S3376:Attribute, EventArgs, and Exception type names should end with the type being extended",
        Justification = "It's a base class so should be suffixed with \"Base\".")]
    public abstract class BrowserAttributeBase : DataAttribute
    {
        protected abstract Browser Browser { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new[] { Browser as object };
        }
    }
}
