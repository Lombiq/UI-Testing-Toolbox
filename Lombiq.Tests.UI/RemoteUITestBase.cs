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
public abstract class RemoteUITestBase : UITestBase
{
    public Uri BaseUri { get; set; }

    protected override WebApplicationInstanceFactory InstanceFactory { get; }

    protected RemoteUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        InstanceFactory = (_, _) => new RemoteInstance(BaseUri);
        AddTest(context => context.GoToAbsoluteUrlAsync(BaseUri));
    }

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Action<UITestContext> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(baseUri, testAsync.AsCompletedTask(), changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(baseUri, testAsync, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(baseUri, testAsync, default, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        BaseUri = baseUri;
        Browser = browser;

        AddConfiguration(changeConfigurationAsync);
        AddTest(testAsync);

        return RunAsync();
    }

    public override Task RunAsync()
    {
        AddSyncConfiguration(configuration =>
        {
            configuration.HtmlValidationConfiguration.HtmlValidationAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
            configuration.AccessibilityCheckingConfiguration.AccessibilityCheckingAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
            configuration.TestDumpConfiguration.CaptureAppSnapshot = false;
        });
        return base.RunAsync();
    }
}
