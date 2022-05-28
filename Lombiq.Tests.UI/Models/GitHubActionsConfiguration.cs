using System;

namespace Lombiq.Tests.UI.Models;

public class GitHubActionsConfiguration
{
    public static Lazy<bool> IsGitHubEnvironment { get; } = new(() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ENV")));

    public string TargetFileNamespace { get; set; }
}
