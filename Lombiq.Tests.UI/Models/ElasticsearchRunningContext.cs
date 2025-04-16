using Elasticsearch.Net;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Environment.Shell.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(Guid Id, string Prefix)
{
    private IndexName IndexName => Indices.Index($"{Prefix}_*");

    public Task BeforeTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async provider =>
        {
            var index = IndexName;
            var cancellation = context.Configuration.TestCancellationToken;

            if (GetClient(provider) is not { } client)
            {
                context.Scope?.AtataContext?.Log?.Debug(
                    $"Couldn't resolve {nameof(IElasticClient)} while waiting for \"{index}\".");
                return;
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
                WithPrefixElasticsearchIndexCleanupFinallyAsync(provider, context, IndexName));
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

    private static IElasticClient GetClient(IServiceProvider provider)
    {
        if ((provider.GetService<IElasticClient>() ?? provider.GetService<ElasticClient>()) is { } service)
        {
            return service;
        }

        var shellConfiguration = provider.GetRequiredService<IShellConfiguration>();
        return shellConfiguration.CreateElasticClient();
    }
}
