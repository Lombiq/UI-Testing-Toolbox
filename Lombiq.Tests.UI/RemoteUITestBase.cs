using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

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

        configuration.HtmlValidationConfiguration.HtmlValidationAndAssertionOnPageChangeRule = (_) => true;
        configuration.AccessibilityCheckingConfiguration.AccessibilityCheckingAndAssertionOnPageChangeRule = (_) => true;
        configuration.FailureDumpConfiguration.CaptureAppSnapshot = false;

        if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);

        await ExecuteOrchardCoreTestAsync((_, _) => new RemoteInstance(baseUri), testManifest, configuration);
    }
}
