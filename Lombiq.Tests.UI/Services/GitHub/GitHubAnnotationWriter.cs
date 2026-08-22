#nullable enable

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Services.GitHub;

public class GitHubAnnotationWriter
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GitHubAnnotationWriter(ITestOutputHelper testOutputHelper) =>
        _testOutputHelper = testOutputHelper;

    public void Annotate(LogLevel severity, string? title, string message, string? file = null, int line = 1)
    {
        ArgumentNullException.ThrowIfNull(message);

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

        // We replace commas with reversed commas and double colons with the squared four dots character to avoid
        // conflicts with the command parser. These are reasonably similar to carry the meaning, yet distinct enough to
        // avoid misleading the reader. (For example if we replaced colons with "Armenian full stop" that looks
        // identical, the user would have no idea why copying the output to a search yields no results when it should.)
        title = title == null ? severity.ToString() : title.Replace(',', '⹁').Replace("::", "⸬");

        // Sanitize message:
        message = message.Replace("\r", string.Empty).Replace('\n', ' ');

        // We don't use the annotation "file" and "line" parameters, because if the file is not in the repo (e.g. it's
        // in a submodule) then the annotation will not display at all.
        if (!string.IsNullOrWhiteSpace(file))
        {
            message = string.Create(CultureInfo.InvariantCulture, $"(file={file},line={line}) {message}");
        }

        _testOutputHelper.WriteLine($"::{command} title={title}::{message}");
    }

    public void ErrorInTest(Exception exception, ITestCase testCase)
    {
        var className = testCase.TestMethod!.TestClass.TestClassName.Split('.')[^1];
        var testName = testCase.TestMethod.MethodName;

        var stackFrames = new StackTrace(exception, fNeedFileInfo: true)
            .GetFrames()
            .Where(frame => frame.GetFileName() != null)
            .ToList();
        var stackFrame =
            stackFrames.Find(frame =>
                frame.GetMethod() is { } method &&
                method.Name == testName &&
                method.DeclaringType?.Name == className) ??
            stackFrames.Find(frame => frame.GetMethod()?.DeclaringType?.FullName?.Contains(className) == true) ??
            stackFrames.FirstOrDefault();
        var file = stackFrame?.GetFileName();
        var line = stackFrame?.GetFileLineNumber() ?? 1;

        Annotate(
            LogLevel.Error,
            $"{exception.GetType().Name} in {testCase.TestCaseDisplayName}",
            exception.ToString(),
            file,
            line);
    }
}
