using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Lombiq.Tests.UI.Extensions;

public static class TestContextExtensions
{
    /// <summary>
    /// Gets a <see langword="string"/> which is safe to use as an Elasticsearch index.
    /// </summary>
    /// <param name="id">
    /// A unique identifier that stays the same between setup and test. This ensures that leftover data in the test
    /// won't be confused with previous runs.
    /// </param>
    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Elasticsearch indexes are lowercase only.")]
    public static string GetElasticserachSafeIndexName(this ITestContext context, Guid id)
    {
        var name = context?
            .Test?
            .TestDisplayName?
            .ToLowerInvariant()
            .RegexReplace("[^a-z0-9]+", "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(name)) return id.ToString("N");

        // An Elasticsearch index can't be longer than 255 character, but that includes the test name, tenant name, GUID
        // and relative index name. So altogether 100 characters is a reasonable limit for the test name prefix.
        if (name.Length > 100) name = name[..100];

        return $"{name}-{id:N}";
    }
}
