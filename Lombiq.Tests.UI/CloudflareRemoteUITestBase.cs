using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

/// <summary>
/// Base class for UI tests that run on a remote (i.e. not locally running) app behind Cloudflare. Gets around
/// Cloudflare rejecting automated requests.
/// </summary>
public abstract class CloudflareRemoteUITestBase : RemoteUITestBase
{
    /// <summary>
    /// Gets the Cloudflare account's ID, necessary for Cloudflare API calls. Note that due to how the IP Access Rule
    /// management works, you may only have tests for apps behind a single Cloudflare account in a given test project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can look up your Cloudflare account's ID from the Cloudflare dashboard following the <see
    /// href="https://developers.cloudflare.com/fundamentals/setup/find-account-and-zone-ids/">Cloudflare
    /// documentation</see>.
    /// </para>
    /// </remarks>
    protected abstract string CloudflareAccountId { get; }

    /// <summary>
    /// Gets the Cloudflare API token to use for setting up IP Access Rules, so the machine running the tests won't get
    /// its requests rejected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create an API token following the <see
    /// ref="https://developers.cloudflare.com/fundamentals/api/get-started/create-token/">Cloudflare
    /// documentation</see>. It only has to have the account-level Account Firewall Access Rules permission with Edit
    /// rights.
    /// </para>
    /// <para>
    /// You can configure the API token by overriding this property in your test class and setting it from any custom
    /// environment value, or by setting the default <c>Lombiq_Tests_UI__CloudflareApiToken</c> environment variable. We
    /// don't recommend hard-coding the token.
    /// </para>
    /// </remarks>
    protected virtual string CloudflareApiToken => TestConfigurationManager.GetConfiguration("CloudflareApiToken");

    protected CloudflareRemoteUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected override Task ExecuteTestAsync(
        Uri baseUri,
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        async Task ChangeConfigurationForCloudflareAsync(OrchardCoreUITestExecutorConfiguration configuration)
        {
            // Cloudflare's e-mail address obfuscating feature creates invalid iframes.
            configuration.HtmlValidationConfiguration.WithRelativeConfigPath("PermitNoTitleIframes.htmlvalidate.json");

            if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);
        }

        if (string.IsNullOrEmpty(CloudflareApiToken))
        {
            _testOutputHelper.WriteLineTimestampedAndDebug(
                "No Cloudflare API token is set, thus skipping the IP Access Rule setup. Note that Cloudflare might " +
                "reject automated requests with HTTP 403s.");

            return base.ExecuteTestAsync(baseUri, testAsync, browser, ChangeConfigurationForCloudflareAsync);
        }

        return CloudflareHelper.ExecuteWrappedInIpAccessRuleManagementAsync(
            () => base.ExecuteTestAsync(baseUri, testAsync, browser, ChangeConfigurationForCloudflareAsync),
            CloudflareAccountId,
            CloudflareApiToken);
    }
}
