using Elasticsearch.Net;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(Guid Id, string Prefix)
{
    /// <summary>
    /// Gets the expression that refers to all indexes that starts with <see cref="Prefix"/>. This should only be used
    /// with <see cref="IElasticClient"/>, because the OrchardCore-specific services automatically apply the prefix from
    /// configuration so it would result in double prefixing.
    /// </summary>
    private IndexName LowLevelIndexName => Indices.Index($"{Prefix}_*");

    public Task BeforeTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async provider =>
        {
            var index = LowLevelIndexName;
            var cancellation = context.Configuration.TestCancellationToken;

            if (GetClient(provider) is not { } client)
            {
                throw new InvalidOperationException(
                    $"Couldn't resolve {nameof(IElasticClient)} while waiting for \"{index}\".");
            }

            (await client.Indices.FlushAsync(index, ct: cancellation)).ThrowIfFailed($"flush index \"{index}\"");
            (await client.Indices.RefreshAsync(index, ct: cancellation)).ThrowIfFailed($"refresh index \"{index}\"");
        });

    public Task AfterTestAsync(UITestContext context) =>
        context?.Application?.Services is { } ? AfterTestInnerAsync(context) : Task.CompletedTask;

    private async Task AfterTestInnerAsync(UITestContext context)
    {
        try
        {
            await context.Application.UsingScopeAsync(provider =>
                WithPrefixElasticsearchIndexCleanupFinallyAsync(provider, context, LowLevelIndexName));
        }
        catch (Exception inner)
        {
            context.Scope?.AtataContext?.Log?.Error(inner.ToString());
        }
    }

    [SuppressMessage(
        "Usage",
        "MA0040:Forward the CancellationToken parameter to methods that take one",
        Justification = "Cleanup code has no viable cancellation token because even failed tests should be cleaned up.")]
    private static async Task WithPrefixElasticsearchIndexCleanupFinallyAsync(
        IServiceProvider provider,
        UITestContext context,
        IndexName index)
    {
        static async Task<bool> CheckIfIndexExistsAsync(IElasticClient client, IndexName index)
        {
            var indices = (await client.Indices.GetAsync(index, ct: CancellationToken.None))
                .ThrowIfFailed($"query index \"{index}\"");
            return indices.Indices.Count > 0;
        }

        if (GetClient(provider) is not { } client)
        {
            throw new InvalidOperationException(
                $"Couldn't resolve {nameof(IElasticClient)} while attempting to clean up \"{index}\".");
        }

        if (!await CheckIfIndexExistsAsync(client, index))
        {
            context.Scope?.AtataContext?.Log?.Warn("No Elasticsearch indexes were found.");
            return;
        }

        var deleteRequest = new DeleteIndexRequest(index) { ExpandWildcards = ExpandWildcards.All };
        (await client.Indices.DeleteAsync(deleteRequest)).ThrowIfFailed($"delete index \"{index}\"");

        if (await CheckIfIndexExistsAsync(client, index))
        {
            throw new InvalidOperationException($"Couldn't delete indexes for \"{index.Name}\".");
        }
    }

    private static IElasticClient GetClient(IServiceProvider provider) =>
        provider.GetService<IElasticClient>() ?? provider.GetService<ElasticClient>();
}
