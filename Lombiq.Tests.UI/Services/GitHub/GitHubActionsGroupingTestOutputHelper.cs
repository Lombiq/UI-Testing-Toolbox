using Lombiq.Tests.UI.Models;
using System;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services.GitHub;

internal sealed class GitHubActionsGroupingTestOutputHelper : ITestOutputHelperDecorator
{
    private readonly string _groupName;

    private bool _isStarted;

    public ITestOutputHelper Decorated { get; private set; }

    private GitHubActionsGroupingTestOutputHelper(ITestOutputHelper decorated, string groupName)
    {
        Decorated = decorated;
        _groupName = groupName;
    }

    public void WriteLine(string message)
    {
        Start();
        Decorated.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        Start();
        Decorated.WriteLine(format, args);
    }

    private void Start()
    {
        if (_isStarted) return;

        Decorated.WriteLine($"::group::{_groupName}");
        _isStarted = true;
    }

    private void EndGroup()
    {
        if (_isStarted) Decorated.WriteLine("::endgroup::");
    }

    public static (ITestOutputHelper DecoratedOutputHelper, Action AfterTest) CreateDecorator(
        ITestOutputHelper testOutputHelper,
        UITestManifest testManifest)
    {
        if (!GitHubHelper.IsGitHubEnvironment ||
            testManifest.XunitTest?.TestCase?.TestMethod?.TestClass?.Class?.Name is not { } className ||
            testManifest.Name is not { } testName)
        {
            return (testOutputHelper, () => { });
        }

        var gitHubActionsGroupingTestOutputHelper = new GitHubActionsGroupingTestOutputHelper(
            testOutputHelper,
            $"{className}.{testName}");

        return (gitHubActionsGroupingTestOutputHelper, () => gitHubActionsGroupingTestOutputHelper.EndGroup());
    }
}
