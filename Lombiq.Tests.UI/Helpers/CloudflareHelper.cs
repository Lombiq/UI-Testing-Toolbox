using Refit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Helpers;

internal static class CloudflareHelper
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly ConcurrentDictionary<string, int> _referenceCounts = new();
    private static readonly ConcurrentDictionary<string, string> _ipAccessRuleIds = new();
    private static ICloudflareApi _cloudflareApi;

    /// <summary>
    /// Executes a while maintaining a Cloudflare IP Access Rule allowing the current IP address to access the site
    /// without getting an anti-bot challenge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Maintaining references is necessary because a) we want to create only the minimally necessary Rules (i.e. no
    /// separate rule per test, to keep API communication to a minimum) and b) we can't know when the whole test suite
    /// starts and ends, only when individual tests do.
    /// </para>
    /// </remarks>
    public static async Task ExecuteWrappedInIpAccessRuleManagementAsync(
        Func<Task> testAsync,
        string cloudflareAccountId,
        string cloudflareApiToken,
        ITestOutputHelper testOutputHelper,
        CancellationToken cancellationToken)
    {
        var currentIp = await GetPublicIpAsync(cancellationToken);
        // The IP retrieved above and the one later seen by the app during browser interactions can be different for
        // some reason (perhaps different network routing). So, instead of creating a Cloudflare IP Access Rule just for
        // the specific IP, we create it for the whole /24 subnet (i.e. X.Y.Z.*). In our experience, that always covers
        // it, since the IPs are usually close to each other.
        var currentIpRange = currentIp[0..currentIp.LastIndexOf('.')] + ".0/24";

        testOutputHelper.WriteLineTimestampedAndDebug(
            "Current public IP address of the runner is {0}. Using IP range {0}.", currentIp, currentIpRange);

        testOutputHelper.WriteLineTimestampedAndDebug(
            "Current Cloudflare IP Access Rule reference count for IP {0} before entering semaphore: {1}.",
            currentIpRange,
            _referenceCounts.GetOrAdd(currentIpRange, 0));

        await _semaphore.WaitAsync(cancellationToken);
        _referenceCounts.AddOrUpdate(currentIpRange, 1, (_, count) => count + 1);

        testOutputHelper.WriteLineTimestampedAndDebug(
            "Current Cloudflare IP Access Rule reference count for IP {0} after entering semaphore: {1}.",
            currentIpRange,
            _referenceCounts[currentIpRange]);

        try
        {
            _cloudflareApi ??= RestService.For<ICloudflareApi>("https://api.cloudflare.com/client/v4", new RefitSettings
            {
                AuthorizationHeaderValueGetter = (_, _) => Task.FromResult(cloudflareApiToken),
            });

            if (!_ipAccessRuleIds.ContainsKey(currentIpRange))
            {
                testOutputHelper.WriteLineTimestampedAndDebug("Creating a Cloudflare IP Access Rule for the IP {0}.", currentIpRange);

                // Delete any pre-existing rules for the current IP first.
                string preexistingRuleId = null;
                await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await _cloudflareApi.GetIpAccessRulesAsync(cloudflareAccountId, currentIpRange);
                        preexistingRuleId = rulesResponse.Result?.FirstOrDefault()?.Id;
                        return rulesResponse.Success;
                    },
                    cancellationToken: cancellationToken);

                // preexistingRuleId can be set in the delegate above, so it's not always null.
#pragma warning disable S2583 // Conditionally executed code should be reachable
                if (preexistingRuleId != null)
                {
                    await DeleteIpAccessRuleWithRetriesAsync(cloudflareAccountId, preexistingRuleId, cancellationToken);
                }
#pragma warning restore S2583 // Conditionally executed code should be reachable

                // Create the IP Access Rule.
                var createResponseResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var createResponse = await _cloudflareApi.CreateIpAccessRuleAsync(cloudflareAccountId, new IpAccessRuleRequest
                        {
                            Mode = "whitelist",
                            Configuration = new IpAccessRuleConfiguration { Target = "ip_range", Value = currentIpRange },
                            Notes = "Temporarily allow a remote UI test from GitHub Actions.",
                        });

                        _ipAccessRuleIds[currentIpRange] = createResponse.Result?.Id;

                        return createResponse.Success && _ipAccessRuleIds[currentIpRange] != null;
                    },
                    cancellationToken: cancellationToken);

                ThrowIfNotSuccess(createResponseResult, currentIpRange, "didn't save properly");

                // Wait for the rule to appear, to make sure that it's active.
                var ruleCheckRequestResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
                    async () =>
                    {
                        var rulesResponse = await _cloudflareApi.GetIpAccessRulesAsync(cloudflareAccountId);
                        return rulesResponse.Success && rulesResponse.Result.Exists(rule => rule.Id == _ipAccessRuleIds[currentIpRange]);
                    },
                    cancellationToken: cancellationToken);

                ThrowIfNotSuccess(ruleCheckRequestResult, currentIpRange, "didn't get activated");

                testOutputHelper.WriteLineTimestampedAndDebug(
                    "Created a Cloudflare IP Access Rule for the IP {0} (Rule ID: {1}).", currentIpRange, _ipAccessRuleIds[currentIpRange]);
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
            testOutputHelper.WriteLineTimestampedAndDebug(
                "Current Cloudflare IP Access Rule reference count for IP {0} after the test (including this test): {1}.",
                currentIpRange,
                _referenceCounts[currentIpRange]);

            // Clean up the IP access rule.
            if (_ipAccessRuleIds.TryGetValue(currentIpRange, out string oldIpAccessRuleId) &&
                _referenceCounts.AddOrUpdate(currentIpRange, 0, (_, count) => count - 1) == 0)
            {
                testOutputHelper.WriteLineTimestampedAndDebug(
                    "Removing the Cloudflare IP Access Rule for the IP {0} (Rule ID: {1}) since this test has the last reference to it.",
                    currentIpRange,
                    oldIpAccessRuleId);

                var deleteSucceededResult = await DeleteIpAccessRuleWithRetriesAsync(cloudflareAccountId, oldIpAccessRuleId, cancellationToken);

                if (deleteSucceededResult.IsSuccess) _ipAccessRuleIds.TryRemove(currentIpRange, out _);

                ThrowIfNotSuccess(deleteSucceededResult, currentIpRange, "couldn't be deleted");

                testOutputHelper.WriteLineTimestampedAndDebug(
                    "Removed the Cloudflare IP Access Rule for the IP {0} (Rule ID: {1}) since this test had the last reference to it.",
                    currentIpRange,
                    oldIpAccessRuleId);
            }
            else
            {
                testOutputHelper.WriteLineTimestampedAndDebug(
                    "Not removing the Cloudflare IP Access Rule for the IP {0} (Rule ID: {1}) since the current reference count is NOT 0.",
                    currentIpRange,
                    _ipAccessRuleIds[currentIpRange]);
            }
        }
    }

    private static async Task<string> GetPublicIpAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        string ip = string.Empty;

        var ipRequestResult = await ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                var response = await client.GetStringAsync("https://cloudflare.com/cdn-cgi/trace", cancellationToken);
                var lines = response.Split('\n');
                var ipLine = Array.Find(lines, line => line.StartsWithOrdinalIgnoreCase("ip="));
                ip = ipLine?.Split('=')[1];
                return !string.IsNullOrEmpty(ip);
            },
            cancellationToken: cancellationToken);

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
        string ipAccessRuleId,
        CancellationToken cancellationToken) =>
        ReliabilityHelper.DoWithRetriesAndCatchesAsync(
            async () =>
            {
                var deleteResponse = await _cloudflareApi.DeleteIpAccessRuleAsync(cloudflareAccountId, ipAccessRuleId);
                return deleteResponse.Success;
            },
            cancellationToken: cancellationToken);

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
            [AliasAs("configuration.value")] string configurationValue = null, // codespell:ignore
            [AliasAs("per_page")] int pageSize = 200); // codespell:ignore

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
