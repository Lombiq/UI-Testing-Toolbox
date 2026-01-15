using Lombiq.Tests.UI.Models;
using System;
using Xunit;

namespace Lombiq.Tests.UI.Services.GitHub;

internal sealed class GitHubActionsGroupingTestOutputHelper : ITestOutputHelperDecorator
{
    private readonly string _groupName;

    private bool _isStarted;

    public ITestOutputHelper Decorated { get; private set; }

    public string Output => Decorated.Output;

    private GitHubActionsGroupingTestOutputHelper(ITestOutputHelper decorated, string groupName)
    {
        Decorated = decorated;
        _groupName = groupName;
    }

    public void Write(string message)
    {
        StartGroupIfNotStarted();
        Decorated.Write(message);
    }

    public void Write(string format, params object[] args)
    {
        StartGroupIfNotStarted();
        Decorated.Write(format, args);
    }

    public void WriteLine(string message)
    {
        StartGroupIfNotStarted();
        Decorated.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        StartGroupIfNotStarted();
        Decorated.WriteLine(format, args);
    }

    private void StartGroupIfNotStarted()
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
            testManifest.XunitTest?.TestCase?.TestMethod?.TestClass?.TestClassName is not { } className ||
            testManifest.Name is not { } testName)
        {
            return (testOutputHelper, () => { });
        }

        var gitHubActionsGroupingTestOutputHelper = new GitHubActionsGroupingTestOutputHelper(
            testOutputHelper,
            $"{className}.{testName}");

        return (gitHubActionsGroupingTestOutputHelper, gitHubActionsGroupingTestOutputHelper.EndGroup);
    }
}
