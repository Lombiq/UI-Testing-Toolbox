using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Lombiq.Tests.UI.Extensions;

public static class TestContextExtensions
{
    /// <summary>
    /// Gets a <see langword="string"/> which is safe to use as an Elasticsearch index.
    /// </summary>
    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Elasticsearch indexes are lowercase only.")]
    public static string GetElasticserachSafeIndexName(this ITestContext context)
    {
        var name = context?
            .Test?
            .TestDisplayName?
            .ToLowerInvariant()
            .RegexReplace("[^a-z0-9]+", "-")
            .Trim('-');

        if (string.IsNullOrEmpty(name)) return Guid.NewGuid().ToString("N");

        return name.Length > 255
            ? name[..(255 - 32)] + Guid.NewGuid().ToString("N")
            : name;
    }
}
