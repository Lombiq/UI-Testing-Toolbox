using Elasticsearch.Net;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OrchardCore.Environment.Shell.Scope;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class TestFuncExtensions
{
    /// <summary>
    /// Wraps the provided <paramref name="testAsync"/> async function to always clean up the Elasticsearch indexes that
    /// start with <paramref name="prefix"/> at the end of the execution, regardless whether it succeeded or not. It
    /// also forcefully flushes the Elasticsearch cache before executing <paramref name="testAsync"/>, to ensure that
    /// all indexes are available.
    /// </summary>
    public static Func<UITestContext, Task> WithPrefixElasticsearchFlushAndCleanup(
        this Func<UITestContext, Task> testAsync,
        string prefix)
    {
        var index = Indices.Index($"{prefix}_*");

        return async context =>
        {
            Exception exception = null;
            var cancellation = context.Configuration.TestCancellationToken;

            try
            {
                // Before starting the test, force the index to flush and refresh to ensure that all data are available for search. This is a
                // resource-intensive process, but it's necessary to explicitly force it, because these test indexes are short-lived by nature.
                await context.Application.UsingScopeAsync(async shellScope =>
                {
                    var client = shellScope.ServiceProvider.GetRequiredService<IElasticClient>();
                    (await client.Indices.FlushAsync(index, ct: cancellation)).ThrowIfFailed($"flush index \"{index}\"");
                    (await client.Indices.RefreshAsync(index, ct: cancellation)).ThrowIfFailed($"refresh index \"{index}\"");
                });

                await testAsync(context);
            }
            finally
            {
                await context.Application.UsingScopeAsync(async scope =>
                {
                    try
                    {
                        await WithPrefixElasticsearchIndexCleanupFinallyAsync(scope, context, index);
                    }
                    catch (Exception inner)
                    {
                        context.Scope?.AtataContext?.Log?.Error(inner.ToString());
                        exception = inner;
                    }
                });
            }

            if (exception != null) throw exception;
        };
    }

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
