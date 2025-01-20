using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Models;

/// <summary>
/// Provides data about the currently executing test.
/// </summary>
public class UITestManifest
{
    public ITest XunitTest => TestContext.Current.Test;
    public string Name => XunitTest.TestDisplayName;
    public Func<UITestContext, Task> TestAsync { get; private set; }

    public UITestManifest(Func<UITestContext, Task> testAsync) => TestAsync = testAsync;
}
