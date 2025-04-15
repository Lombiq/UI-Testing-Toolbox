using Elasticsearch.Net;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Environment.Shell.Scope;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record ElasticsearchRunningContext(Guid Id, string Prefix)
{
    private IndexName IndexName => Indices.Index($"{Prefix}_*");

    public Task BeforeTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async shellScope =>
        {
            var index = IndexName;
            var client = shellScope.ServiceProvider.GetRequiredService<IElasticClient>();
            var cancellation = context.Configuration.TestCancellationToken;

            (await client.Indices.FlushAsync(index, ct: cancellation)).ThrowIfFailed($"flush index \"{index}\"");
            (await client.Indices.RefreshAsync(index, ct: cancellation)).ThrowIfFailed($"refresh index \"{index}\"");
        });

    public Task AfterTestAsync(UITestContext context) =>
        context.Application.UsingScopeAsync(async scope =>
        {
            try
            {
                await WithPrefixElasticsearchIndexCleanupFinallyAsync(scope, context, IndexName);
            }
            catch (Exception inner)
            {
                context.Scope?.AtataContext?.Log?.Error(inner.ToString());
            }
        });

    [SuppressMessage(
        "Usage",
        "MA0040:Forward the CancellationToken parameter to methods that take one",
        Justification = "Cleanup code has no viable cancellation token because even failed tests should be cleaned up.")]
    private static async Task WithPrefixElasticsearchIndexCleanupFinallyAsync(
        ShellScope shellScope,
        UITestContext context,
        IndexName index)
    {
        static async Task<bool> CheckIfIndexExistsAsync(IElasticClient client, IndexName index)
        {
            var indices = (await client.Indices.GetAsync(index, ct: CancellationToken.None))
                .ThrowIfFailed($"query index \"{index}\"");
            return indices.Indices.Count > 0;
        }

        var client = shellScope.ServiceProvider.GetRequiredService<IElasticClient>();

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
}
