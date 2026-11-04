#nullable enable

using Atata;
using System;
using Xunit;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Necessary to prevent long paths, an issue under Windows.
/// </summary>
public class ContextIdArtifactsPathFactory : IArtifactsPathFactory
{
    public static IArtifactsPathFactory Instance { get; } = new ContextIdArtifactsPathFactory();

    public string Create(AtataContext context) =>
        TestContext.Current.KeyValueStorage[nameof(ContextIdArtifactsPathFactory)]?.ToString() ??
        throw new InvalidOperationException(
            $"The {nameof(ContextIdArtifactsPathFactory)} key is missing from the text context's key-value storage.");
}
