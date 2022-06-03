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

    public bool IsStarted { get; private set; }

    public GitHubActionsGroupingTestOutputHelper(ITestOutputHelper inner, string groupName)
    {
        _inner = inner;
        _groupName = groupName;
    }

    public void WriteLine(string message)
    {
        if (!IsStarted) Start();
        _inner.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        if (!IsStarted) Start();
        _inner.WriteLine(format, args);
    }

    public void EndGroup()
    {
        if (IsStarted) _inner.WriteLine("::endgroup::");
    }

    private void Start()
    {
        _inner.WriteLine($"::group::{_groupName}");
        IsStarted = true;
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
