using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI;

public partial class RemoteUITestBase
{
    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    [Obsolete($"Use {nameof(BaseUri)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Action<UITestContext> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(baseUri, testAsync.AsCompletedTask(), changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    [Obsolete($"Use {nameof(BaseUri)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(baseUri, testAsync, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    [Obsolete($"Use {nameof(BaseUri)}, {nameof(AddAsyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(baseUri, testAsync, default, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test on a remote (i.e. not locally running) app.
    /// </summary>
    [Obsolete($"Use {nameof(BaseUri)}, {nameof(Browser)}, {nameof(AddAsyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) => TestBuilder
        .UseBaseUri(baseUri)
        .UseBrowser(browser)
        .AddAsyncConfiguration(changeConfigurationAsync)
        .RunAsync(testAsync);
}
