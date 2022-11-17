namespace Lombiq.Tests.UI.Services.GitHub;

public class GitHubActionsOutputConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether each tests' output should be wrapped into their own groups (<see
    /// href="https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#grouping-log-lines"/>)
    /// in the GitHub Actions output. This only takes effect when tests are executed from a GitHub Actions workflow.
    /// </summary>
    public bool EnablePerTestOutputGrouping { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether errors (exceptions) surfacing from tests should be written as errors (<see
    /// href="https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#example-creating-an-annotation-for-an-error"/>)
    /// in the GitHub Actions output. This only takes effect when tests are executed from a GitHub Actions workflow.
    /// </summary>
    public bool EnableErrorAnnotations { get; set; } = true;
}
