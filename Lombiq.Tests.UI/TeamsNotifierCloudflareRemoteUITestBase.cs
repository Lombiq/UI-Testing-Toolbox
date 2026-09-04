#nullable enable

using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

public abstract class TeamsNotifierCloudflareRemoteUITestBase : CloudflareRemoteUITestBase
{
    protected static bool TeamsMessageWasSent { get; set; }

    protected abstract string SiteName { get; }
    protected abstract Uri BaseUri { get; }
    protected abstract override string CloudflareAccountId { get; }

    protected virtual bool RunIsForProduction => RemoteTestHelper.RunIsForProduction;
    protected virtual string TestFailedTeamsWebhookUrl => TestConfigurationManager.GetConfiguration("TestFailedTeamsWebhookUrl");

    protected TeamsNotifierCloudflareRemoteUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected override async Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task>? changeConfigurationAsync)
    {
        try
        {
            await base.ExecuteTestAsync(
                baseUri,
                testAsync,
                browser,
                async configuration =>
                {
                    configuration.AccessibilityCheckingConfiguration.RunAccessibilityCheckingAssertionOnAllPageChanges = true;

                    if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);
                });
        }
        catch (Exception) when (!TeamsMessageWasSent && RunIsForProduction && !string.IsNullOrEmpty(TestFailedTeamsWebhookUrl))
        {
            TeamsMessageWasSent = true;

            var response = await TeamsHelper.SendFailedUITestTeamsMessageAsync(TestFailedTeamsWebhookUrl, SiteName);

            if (!response.IsSuccessStatusCode)
            {
                _testOutputHelper.WriteLine($"Failed to send message to Teams with the following response: {response}.");
            }

            throw;
        }
    }

    /// <summary>
    /// Execute asynchronous test using <see cref="TeamsNotifierCloudflareRemoteUITestBase"/>.<see cref="BaseUri"/>.
    /// </summary>
    protected Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser = default,
        Func<OrchardCoreUITestExecutorConfiguration, Task>? changeConfigurationAsync = null) =>
        ExecuteTestAsync(BaseUri, testAsync, browser, changeConfigurationAsync ?? (_ => Task.CompletedTask));

    /// <summary>
    /// Execute synchronous test using <see cref="TeamsNotifierCloudflareRemoteUITestBase"/>.<see cref="BaseUri"/>.
    /// </summary>
    protected Task ExecuteSyncTestAsync(
        Action<UITestContext> test,
        Browser browser = default,
        Action<OrchardCoreUITestExecutorConfiguration>? changeConfiguration = null) =>
        ExecuteTestAsync(BaseUri, test.AsCompletedTask(), browser, changeConfiguration.AsCompletedTask());
}
