using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public class GitHubAnnotationWriter
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GitHubAnnotationWriter(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    public void Annotate(LogLevel severity, string title, string message, string file, int line = 1)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(file);

        if (file.Contains(',')) throw new ArgumentException("File name mustn't contain commas.", nameof(file));

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

        _testOutputHelper.WriteLine(FormattableString.Invariant(
            $"::{command} file={file},line={line},title={title}::{message.Replace("\r", string.Empty).Replace("\n", "\\n")}"));
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
            stackFrames.FirstOrDefault();
        var file = stackFrame?.GetFileName() ?? "NoFile";
        var line = stackFrame?.GetFileLineNumber() ?? 1;

        Annotate(LogLevel.Error, exception.GetType().Name, exception.ToString(), file, line);
    }
}
