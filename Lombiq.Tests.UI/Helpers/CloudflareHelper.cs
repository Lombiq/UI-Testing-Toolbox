using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Helpers;

internal static class CloudflareHelper
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static int _referenceCount;

    private static string _currentIp;
    private static string _ipAccessRuleId;
    private static ICloudflareApi _cloudflareApi;

    public static async Task ExecuteWrappedInIpAccessRuleManagementAsync(
        Func<Task> testAsync,
        string cloudflareAccountId,
        string cloudflareApiToken)
    {
        await _semaphore.WaitAsync();
        Interlocked.Increment(ref _referenceCount);

        Debug.WriteLine("Current reference count at the start of the test: {0}.", _referenceCount);

        try
        {
            _cloudflareApi ??= RestService.For<ICloudflareApi>("https://api.cloudflare.com/client/v4", new RefitSettings
            {
                AuthorizationHeaderValueGetter = (_, _) => Task.FromResult(cloudflareApiToken),
            });

            _currentIp ??= _cloudflareApi != null ? await GetPublicIpAsync() : string.Empty;

            if (_ipAccessRuleId == null)
            {
                Debug.WriteLine("Creating an IP Access Rule for the IP {0}.", (object)_currentIp);

                // Delete any pre-existing rules for the current IP first.
                string preexistingRuleId = null;
                await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await _cloudflareApi.GetIpAccessRulesAsync(cloudflareAccountId, _currentIp);
                        preexistingRuleId = rulesResponse.Result?.FirstOrDefault()?.Id;
                        return rulesResponse.Success;
                    });

                // preexistingRuleId can be set in the delegate above, so it's not always null.
#pragma warning disable S2583 // Conditionally executed code should be reachable
                if (preexistingRuleId != null)
                {
                    await DeleteIpAccessRuleWithRetriesAsync(cloudflareAccountId, preexistingRuleId);
                }
#pragma warning restore S2583 // Conditionally executed code should be reachable

                // Create the IP Access Rule.
                var createResponseResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var createResponse = await _cloudflareApi.CreateIpAccessRuleAsync(cloudflareAccountId, new IpAccessRuleRequest
                        {
                            Mode = "whitelist",
                            Configuration = new IpAccessRuleConfiguration { Target = "ip", Value = _currentIp },
                            Notes = "Temporarily allow a remote UI test from GitHub Actions.",
                        });

                        _ipAccessRuleId = createResponse.Result?.Id;

                        return createResponse.Success && _ipAccessRuleId != null;
                    });

                ThrowIfNotSuccess(createResponseResult, _currentIp, "didn't save properly");

                // Wait for the rule to appear, to make sure that it's active.
                var ruleCheckRequestResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await _cloudflareApi.GetIpAccessRulesAsync(cloudflareAccountId);
                        return rulesResponse.Success && rulesResponse.Result.Exists(rule => rule.Id == _ipAccessRuleId);
                    });

                ThrowIfNotSuccess(ruleCheckRequestResult, _currentIp, "didn't get activated");
            }
        }
        finally
        {
            _semaphore.Release();
        }

        try
        {
            await testAsync();
        }
        finally
        {
            // Clean up the IP access rule.
            if (_ipAccessRuleId != null && Interlocked.Decrement(ref _referenceCount) == 0)
            {
                Debug.WriteLine("Removing the IP Access Rule. Current reference count: {0}.", _referenceCount);

                var deleteSucceededResult = await DeleteIpAccessRuleWithRetriesAsync(cloudflareAccountId, _ipAccessRuleId);

                if (deleteSucceededResult.IsSuccess) _ipAccessRuleId = null;

                ThrowIfNotSuccess(deleteSucceededResult, _currentIp, "couldn't be deleted");
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

    public static Task<(bool IsSuccess, Exception Exception)> DeleteIpAccessRuleWithRetriesAsync(
        string cloudflareAccountId,
        string ipAccessRuleId) =>
        ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                var deleteResponse = await _cloudflareApi.DeleteIpAccessRuleAsync(cloudflareAccountId, ipAccessRuleId);
                return deleteResponse.Success;
            });

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
        Task<ApiResponse<IpAccessRuleResponse[]>> GetIpAccessRulesAsync(
            string accountId,
            [AliasAs("configuration.value")] string configurationValue = null,
            [AliasAs("per_page")] int pageSize = 200);

        [Delete("/accounts/{accountId}/firewall/access_rules/rules/{ruleId}")]
        Task<ApiResponse<DeleteResponse>> DeleteIpAccessRuleAsync(string accountId, string ruleId);
    }

    public sealed class IpAccessRuleRequest
    {
        public string Mode { get; set; }
        public IpAccessRuleConfiguration Configuration { get; set; }
        public string Notes { get; set; }
    }

    [DebuggerDisplay("{Target} <- {Value}")]
    public sealed class IpAccessRuleConfiguration
    {
        public string Target { get; set; }
        public string Value { get; set; }
    }

    [DebuggerDisplay("{Id}: {Configuration}")]
    public sealed class IpAccessRuleResponse
    {
        public string Id { get; set; }
        public IpAccessRuleConfiguration Configuration { get; set; }
    }

    public sealed class DeleteResponse
    {
        public bool Success { get; set; }
    }

    public sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public IEnumerable<ApiError> Errors { get; set; }
        public IEnumerable<ApiError> Messages { get; set; }
    }

    public sealed class ApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
