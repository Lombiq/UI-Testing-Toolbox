using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

/// <summary>
/// Base class for UI tests that run on a remote (i.e. not locally running) app. If you're testing an app running behind
/// Cloudflare, then consider using <see cref="CloudflareRemoteUITestBase"/> instead.
/// </summary>
public abstract class RemoteUITestBase : UITestBase
{
    protected RemoteUITestBase(ITestOutputHelper testOutputHelper)
    : base(testOutputHelper)
    {
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
    protected virtual async Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        async Task BaseUriVisitingTest(UITestContext context)
        {
            await context.GoToAbsoluteUrlAsync(baseUri);
            await testAsync(context);
        }

        var testManifest = new UITestManifest(_testOutputHelper) { TestAsync = BaseUriVisitingTest };

        var configuration = new OrchardCoreUITestExecutorConfiguration
        {
            OrchardCoreConfiguration = new OrchardCoreConfiguration(),
            TestOutputHelper = _testOutputHelper,
            BrowserConfiguration = { Browser = browser },
        };

        configuration.HtmlValidationConfiguration.HtmlValidationAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
        configuration.AccessibilityCheckingConfiguration.AccessibilityCheckingAndAssertionOnPageChangeRule = UrlCheckHelper.IsNotOrchardPage;
        configuration.FailureDumpConfiguration.CaptureAppSnapshot = false;
        await changeConfigurationAsync.InvokeFuncAsync(configuration);

        await ExecuteOrchardCoreTestAsync((_, _) => new RemoteInstance(baseUri), testManifest, configuration);
    }
}
