using System;

namespace Lombiq.Tests.UI.Services.GitHub;

public static class GitHubHelper
{
    private static readonly Lazy<bool> _isGitHubEnvironmentLazy = new(() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ENV")));

    public static bool IsGitHubEnvironment => _isGitHubEnvironmentLazy.Value;
}
