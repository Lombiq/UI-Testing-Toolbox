using Lombiq.Tests.UI.Models;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public class GitHubAnnotationWriter
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly GitHubConfiguration _gitHubConfiguration;

    public GitHubAnnotationWriter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gitHubConfiguration = TestConfigurationManager.GetConfiguration<GitHubConfiguration>();
    }

    public void Annotate(LogLevel severity, string title, string message, string file, int line = 1)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(file);

        title ??= severity.ToString();

        var command = severity switch
        {
            LogLevel.Information => "notice",
            LogLevel.Warning => "warning",
            LogLevel.Error => "error",
            LogLevel.Critical => "error",
            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                $"Only {nameof(LogLevel.Information)} - {nameof(LogLevel.Critical)} are valid."),
        };

        var client = new GitHubClient(
            new ProductHeaderValue(_gitHubConfiguration.RepositoryName),
            GitHubClient.GitHubApiUrl);
        if (!string.IsNullOrEmpty(_gitHubConfiguration.Token))
        {
            client.Credentials = new Credentials(_gitHubConfiguration.Token);
        }

        // client.Check.Run.Update(_gitHubConfiguration.RepositoryName)

        _testOutputHelper.WriteLine(FormattableString.Invariant(
            $"::{command} file={file},line={line},title={title}::{message}"));
    }

    public void ErrorInTest(Exception exception, ITestCase testCase)
    {
        var className = testCase.TestMethod.TestClass.Class.Name.Split('.').Last();
        var testName = testCase.TestMethod.Method.Name;

        var stackFrames = new StackTrace(exception, fNeedFileInfo: true)
            .GetFrames()
            .Where(frame => frame.GetFileName() != null)
            .ToList();
        var stackFrame =
            stackFrames.FirstOrDefault(frame =>
                frame.GetMethod() is { } method &&
                method.Name == testName &&
                method.DeclaringType?.Name == className) ??
            stackFrames.FirstOrDefault(frame => frame.GetMethod()?.DeclaringType?.FullName?.Contains(className) == true) ??
            stackFrames.FirstOrDefault();
        var file = stackFrame?.GetFileName() ?? "NoFile";
        var line = stackFrame?.GetFileLineNumber() ?? 1;

        Annotate(
            LogLevel.Error,
            $"{exception.GetType().Name} in {testCase.DisplayName}",
            exception.ToString(),
            file,
            line);
    }
}
