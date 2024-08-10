using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Helpers;

internal static class CloudflareHelper
{
    public static async Task ExecuteWrappedInIpAccessRuleManagementAsync(
        Func<Task> testAsync,
        string cloudflareAccountId,
        string cloudflareApiToken)
    {
        string ipAccessRuleId = null;

        ICloudflareApi cloudflareApi = null;

        cloudflareApi = RestService.For<ICloudflareApi>("https://api.cloudflare.com/client/v4", new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult(cloudflareApiToken),
        });

        var currentIp = cloudflareApi != null ? await GetPublicIpAsync() : string.Empty;

        try
        {
            if (cloudflareApi != null)
            {
                var createResponseResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var createResponse = await cloudflareApi.CreateIpAccessRuleAsync(cloudflareAccountId, new IpAccessRuleRequest
                        {
                            Mode = "whitelist",
                            Configuration = new IpAccessRuleConfiguration { Target = "ip", Value = currentIp },
                            Notes = "Temporarily allow a remote UI test from GitHub Actions.",
                        });

                        ipAccessRuleId = createResponse.Result?.Id;

                        return createResponse.Success && ipAccessRuleId != null;
                    });

                ThrowIfNotSuccess(createResponseResult, currentIp, "didn't save properly");

                // Wait for the rule to appear, to make sure that it's active.
                var ruleRequestResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await cloudflareApi.GetIpAccessRulesAsync(cloudflareAccountId, 100);

                        return rulesResponse.Success && rulesResponse.Result.Exists(rule => rule.Id == ipAccessRuleId);
                    });

                ThrowIfNotSuccess(ruleRequestResult, currentIp, "didn't get activated");
            }

            await testAsync();
        }
        finally
        {
            // Clean up the IP access rule.
            if (ipAccessRuleId != null)
            {
                var deleteSucceededResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var deleteResponse = await cloudflareApi.DeleteIpAccessRuleAsync(cloudflareAccountId, ipAccessRuleId);
                        return deleteResponse.Success;
                    });

                ThrowIfNotSuccess(deleteSucceededResult, currentIp, "couldn't be deleted");
            }
        }
    }

    private static async Task<string> GetPublicIpAsync()
    {
        using var client = new HttpClient();
        string ip = string.Empty;

        var ipRequestResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                ip = await client.GetStringAsync("https://api.ipify.org");
                return true;
            });

        if (!ipRequestResult.IsSuccess)
        {
            throw new IOException("Couldn't get the public IP address of the runner.", ipRequestResult.Exception);
        }

        return ip;
    }

    private static void ThrowIfNotSuccess((bool IsSuccess, Exception InnerException) result, string currentIp, string messagePart)
    {
        if (result.IsSuccess) return;

        throw new IOException(
            $"The Cloudflare IP Access Rule for allowing requests from this runner {messagePart}. There might be a " +
            $"leftover rule for the IP {currentIp} that needs to be deleted manually." +
            (result.InnerException is ApiException ex ? $" Response: {ex.Content}" : string.Empty),
            result.InnerException);
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
