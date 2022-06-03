using Lombiq.Tests.UI.Models;
using System;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

internal sealed class GitHubActionsGroupingTestOutputHelper : ITestOutputHelper
{
    public static Lazy<bool> IsGitHubEnvironment { get; } = new(() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ENV")));

    private readonly ITestOutputHelper _inner;
    private readonly string _groupName;

    private bool _isStarted;

    public GitHubActionsGroupingTestOutputHelper(ITestOutputHelper inner, string groupName)
    {
        _inner = inner;
        _groupName = groupName;
    }

    public void WriteLine(string message)
    {
        Start();
        _inner.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        Start();
        _inner.WriteLine(format, args);
    }

    private void Start()
    {
        if (_isStarted) return;

        // As "::group::name" and "::endgroup::" always has to go to the standard output to be effective these two are
        // intentionally using Console.WriteLine() instead of _inner.WriteLine().
        Console.WriteLine($"::group::{_groupName}");
        _isStarted = true;
    }

    private void EndGroup()
    {
        if (_isStarted) Console.WriteLine("::endgroup::");
    }

    public static (ITestOutputHelper WrappedOutputHelper, Action AfterTest) CreateWrapper(
        ITestOutputHelper testOutputHelper,
        UITestManifest testManifest)
    {
        if (!IsGitHubEnvironment.Value ||
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
