using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
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
    /// Gets the Cloudflare account's ID, as indicated in the account homepage's URL on the Cloudflare dashboard.
    /// </summary>
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

    protected override async Task ExecuteTestAsync(
    Uri baseUri,
    Func<UITestContext, Task> testAsync,
    Browser browser,
    Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        string ipAccessRuleId = null;

        ICloudflareApi cloudflareApi = null;

        if (!string.IsNullOrEmpty(CloudflareApiToken))
        {
            cloudflareApi = RestService.For<ICloudflareApi>("https://api.cloudflare.com/client/v4", new RefitSettings
            {
                AuthorizationHeaderValueGetter = (_, _) => Task.FromResult(CloudflareApiToken),
            });
        }
        else
        {
            _testOutputHelper.WriteLineTimestampedAndDebug(
                "No Cloudflare API token is set, thus skipping the IP Access Rule setup. Note that Cloudflare might " +
                "reject automated requests with HTTP 403s.");
        }

        var currentIp = await GetPublicIpAsync();

        try
        {
            if (cloudflareApi != null)
            {
                var createResponse = await cloudflareApi.CreateIpAccessRuleAsync(CloudflareAccountId, new IpAccessRuleRequest
                {
                    Mode = "whitelist",
                    Configuration = new IpAccessRuleConfiguration { Target = "ip", Value = currentIp },
                    Notes = "Temporarily allow a remote UI test from GitHub Actions.",
                });

                ipAccessRuleId = createResponse.Result?.Id;

                ThrowIfNotSuccess(createResponse.Success && ipAccessRuleId != null, currentIp, "didn't save properly");

                // Wait for the rule to appear, to make sure that it's active.
                var ruleFound = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await cloudflareApi.GetIpAccessRulesAsync(CloudflareAccountId, 100);

                        return rulesResponse.Success && rulesResponse.Result.Exists(rule => rule.Id == ipAccessRuleId);
                    });

                ThrowIfNotSuccess(ruleFound, currentIp, "didn't get activated");
            }

            await base.ExecuteTestAsync(baseUri, testAsync, browser, changeConfigurationAsync);
        }
        finally
        {
            // Clean up the IP access rule.
            if (ipAccessRuleId != null)
            {
                var deleteSucceeded = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var deleteResponse = await cloudflareApi.DeleteIpAccessRuleAsync(CloudflareAccountId, ipAccessRuleId);
                        return deleteResponse.Success;
                    });

                ThrowIfNotSuccess(deleteSucceeded, currentIp, "couldn't be deleted");
            }
        }
    }

    private static async Task<string> GetPublicIpAsync()
    {
        using var client = new HttpClient();
        string ip = string.Empty;

        var ipRequestSucceeded = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                ip = await client.GetStringAsync("https://api.ipify.org");
                return true;
            });

        if (!ipRequestSucceeded)
        {
            throw new IOException("Couldn't get the public IP address of the runner.");
        }

        return ip;
    }

    private static void ThrowIfNotSuccess(bool isSuccess, string currentIp, string messagePart)
    {
        if (isSuccess) return;

        throw new IOException(
            $"The Cloudflare IP Access Rule for allowing requests from this runner {messagePart}. There might be a " +
            $"leftover rule for the IP {currentIp} that needs to be deleted manually.");
    }

    [Headers("Authorization: Bearer")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "It's an API client.")]
    public interface ICloudflareApi
    {
        [Post("/accounts/{accountId}/firewall/access_rules/rules")]
        Task<ApiResponse<IpAccessRuleResponse>> CreateIpAccessRuleAsync(
            string accountId,
            [Body] IpAccessRuleRequest request
        );

        [Get("/accounts/{accountId}/firewall/access_rules/rules")]
        Task<ApiResponse<IpAccessRuleResponse[]>> GetIpAccessRulesAsync(string accountId, [AliasAs("per_page")] int pageSize);

        [Delete("/accounts/{accountId}/firewall/access_rules/rules/{ruleId}")]
        Task<ApiResponse<DeleteResponse>> DeleteIpAccessRuleAsync(string accountId, string ruleId);
    }

    public class IpAccessRuleRequest
    {
        public string Mode { get; set; }
        public IpAccessRuleConfiguration Configuration { get; set; }
        public string Notes { get; set; }
    }

    [DebuggerDisplay("{Target} <- {Value}")]
    public class IpAccessRuleConfiguration
    {
        public string Target { get; set; }
        public string Value { get; set; }
    }

    [DebuggerDisplay("{Id}: {Configuration}")]
    public class IpAccessRuleResponse
    {
        public string Id { get; set; }
        public IpAccessRuleConfiguration Configuration { get; set; }
    }

    public class DeleteResponse
    {
        public bool Success { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public IEnumerable<ApiError> Errors { get; set; }
        public IEnumerable<ApiError> Messages { get; set; }
    }

    public class ApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
