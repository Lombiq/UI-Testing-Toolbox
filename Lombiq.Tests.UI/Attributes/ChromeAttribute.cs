using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ChromeAttribute : BrowserAttributeBase
{
    protected override Browser Browser => Browser.Chrome;
}
