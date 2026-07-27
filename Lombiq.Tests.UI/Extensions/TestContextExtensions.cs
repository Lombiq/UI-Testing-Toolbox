using System;
using Xunit;

namespace Lombiq.Tests.UI.Extensions;

public static class TestContextExtensions
{
    [Obsolete("Use GetElasticsearchSafeIndexName instead. This method will be removed in a future version.")]
    public static string GetElasticserachSafeIndexName(this ITestContext context, Guid id) =>
        context.GetElasticsearchSafeIndexName();

    /// <summary>
    /// Generates a safe Elasticsearch index name from the test identifiers in <paramref name="context"/>.
    /// </summary>
    public static string GetElasticsearchSafeIndexName(this ITestContext context)
    {
        // Elasticsearch indexes are lowercase only.
#pragma warning disable CA1308 // Normalize strings to uppercase
        var name = context?
            .Test?
            .TestDisplayName?
            .ToLowerInvariant()
            .RegexReplace("[^a-z0-9]+", "-")
            .Trim('-');
#pragma warning restore CA1308 // Normalize strings to uppercase

        var id = context?.Test?.UniqueID?.NullIfWhiteSpace() ?? Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(name)) return id;

        // An Elasticsearch index can't be longer than 255 character, but that includes the test name, tenant name, GUID
        // and relative index name. So altogether 100 characters is a reasonable limit for the test name prefix.
        if (name.Length > 100) name = name[..100];

        return $"{name}-{id}";
    }
}
