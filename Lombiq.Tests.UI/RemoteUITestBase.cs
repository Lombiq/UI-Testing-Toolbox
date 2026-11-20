using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

/// <summary>
/// Base class for UI tests that run on a remote (i.e. not locally running) app. If you're testing an app running behind
/// Cloudflare, then consider using <see cref="CloudflareRemoteUITestBase"/> instead.
/// </summary>
public abstract partial class RemoteUITestBase : UITestBase
{
    protected RemoteUITestBase TestBuilder => this;

    public virtual Uri BaseUri { get; set; }

    protected override WebApplicationInstanceFactory InstanceFactory { get; }

    protected RemoteUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        InstanceFactory = (_, _) => new RemoteInstance(BaseUri);
        AddTest(context => context.GoToAbsoluteUrlAsync(BaseUri));
    }

    public RemoteUITestBase UseBaseUri(Uri baseUri)
    {
        BaseUri = baseUri;
        return this;
    }

    protected override void Configure(OrchardCoreUITestExecutorConfiguration configuration)
    {
        base.Configure(configuration);

        configuration.HtmlValidationConfiguration.HtmlValidationAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
        configuration.AccessibilityCheckingConfiguration.AccessibilityCheckingAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
        configuration.TestDumpConfiguration.CaptureAppSnapshot = false;
    }

    /// <summary>
    /// Adds a synchronous <paramref name="test"/>. If <paramref name="browser"/> is not <see langword="null"/>, sets
    /// <see cref="Browser"/> to its value. Then executes the test.
    /// </summary>
    public Task RunAsync(Action<UITestContext> test, Browser? browser)
    {
        if (browser.HasValue) Browser = browser.Value;

        return RunAsync(test.AsCompletedTask());
    }
}
