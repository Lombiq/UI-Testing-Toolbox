using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class InternetExplorerAttribute : BrowserAttributeBase
{
    protected override Browser Browser => Browser.InternetExplorer;
}
