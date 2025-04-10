using Elasticsearch.Net;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class TestFuncExtensions
{
    /// <summary>
    /// Wraps the provided <paramref name="testAsync"/> async function to always clean up the Elasticsearch indexes that
    /// start with <paramref name="prefix"/> at the end of the execution, regardless whether it succeeded or not.
    /// </summary>
    [SuppressMessage(
        "Usage",
        "MA0040:Forward the CancellationToken parameter to methods that take one",
        Justification = "Cleanup code has no viable cancellation token because even failed tests should be cleaned up.")]
    public static Func<UITestContext, Task> WithPrefixElasticsearchIndexCleanup(
        this Func<UITestContext, Task> testAsync,
        string prefix) =>
        async context =>
        {
            InvalidOperationException exception = null;
            try
            {
                await testAsync(context);
            }
            finally
            {
                await context.Application.UsingScopeAsync(async shellScope =>
                {
                    var client = shellScope.ServiceProvider.GetRequiredService<IElasticClient>();
                    var query = $"{prefix}_*";

                    (exception, var indices) = await CheckIfIndexExistsAsync(context, client, query);
                    if (exception != null) return;

                    if (indices.Indices.Count == 0)
                    {
                        context.Scope?.AtataContext?.Log?.Warn("No Elasticsearch indexes were found.");
                    }

                    var deleted = await client.Indices.DeleteAsync(
                        new DeleteIndexRequest(query) { ExpandWildcards = ExpandWildcards.All });
                    if (!deleted.IsValid)
                    {
                        exception = new(deleted.ToString());
                        context.Scope?.AtataContext?.Log?.Error(exception.ToString());
                    }

                    (exception, indices) = await CheckIfIndexExistsAsync(context, client, query);
                    if (exception != null) return;
                    if (indices.Indices.Count > 0)
                    {
                        exception = new($"Couldn't delete indexes for prefix \"{prefix}\".");
                        context.Scope?.AtataContext?.Log?.Error(exception.ToString());
                    }
                });
            }

            if (exception != null) throw exception;
        };

    private static async Task<(InvalidOperationException Exception, GetIndexResponse Response)> CheckIfIndexExistsAsync(
        UITestContext context,
        IElasticClient client,
        string query)
    {

        var indices = await client.Indices.GetAsync(Indices.Index(query), ct: CancellationToken.None);
        if (indices.IsValid) return (null, indices);

        var exception = new InvalidOperationException("Failed to query Elasticserach indexes. " + indices);
        context.Scope?.AtataContext?.Log?.Error(exception.ToString());
        return (exception, indices);
    }
}
