using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

public abstract class TeamsNotifierCloudflareRemoteUITestBase : CloudflareRemoteUITestBase
{
    private static bool _teamsMessageWasSent;

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
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
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
        catch (Exception) when (!_teamsMessageWasSent && RunIsForProduction && !string.IsNullOrEmpty(TestFailedTeamsWebhookUrl))
        {
            _teamsMessageWasSent = true;

            var isSuccessful = await TeamsHelper.SendFailedUiTestTeamsMessageAsync(TestFailedTeamsWebhookUrl, SiteName);

            if (!isSuccessful.IsSuccessStatusCode)
            {
                _testOutputHelper.WriteLine($"Failed to send message to Teams with the following response: {isSuccessful}.");
            }

            throw;
        }
    }
}
