using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EdgeAttribute : BrowserAttributeBase
{
    protected override Browser Browser => Browser.Edge;
}
