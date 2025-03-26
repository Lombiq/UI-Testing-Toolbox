using Elasticsearch.Net;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class TestFuncExtensions
{
    /// <summary>
    /// Wraps the provided <paramref name="testAsync"/> async function to always clean up the Elasticsearch indexes that
    /// start with <paramref name="prefix"/> at the end of the execution, regardless whether it succeeded or not.
    /// </summary>
    public static Func<UITestContext, Task> WithPrefixElasticsearchIndexCleanup(
        this Func<UITestContext, Task> testAsync,
        string prefix) =>
        async context =>
        {
            try
            {
                await testAsync(context);
            }
            finally
            {
                await context.Application.UsingScopeAsync(shellScope =>
                {
                    var client = shellScope.ServiceProvider.GetRequiredService<IElasticClient>();

                    return client.Indices.DeleteAsync(
                        new DeleteIndexRequest(prefix + "*")
                        {
                            ExpandWildcards = ExpandWildcards.All,
                            AllowNoIndices = true,
                        },
                        CancellationToken.None);
                });
            }
        };
}
