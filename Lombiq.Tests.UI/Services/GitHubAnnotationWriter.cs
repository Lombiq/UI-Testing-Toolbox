using Lombiq.Tests.UI.Models;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit.Abstractions;
using FileMode = System.IO.FileMode;

namespace Lombiq.Tests.UI.Services;

public class GitHubAnnotationWriter
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GitHubAnnotationWriter(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    public void Annotate(LogLevel severity, string title, string message, string file, int line = 1)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(file);

        // The workflow command uses commas to separate the arguments (see last line of this method) so if the file name
        // contained a comma, the part after the comma would be chopped off.
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

        // We replace commas with reversed commas and double colons with the squared four dots character to avoid
        // conflicts with the command parser. These are reasonably similar to carry the meaning, yet distinct enough to
        // avoid misleading the reader. (For example if we replaced colons with "Armenian full stop" that looks
        // identical, the user would have no idea why copying the output to a search yields no results when it should.)
        title = title.Replace(',', '⹁').Replace("::", "⸬");

        // Sanitize message:
        message = message.Replace("\r", string.Empty).Replace("\n", " ");

        _testOutputHelper.WriteLine(FormattableString.Invariant(
            $"::{command} file={file},line={line},title={title}::{message}"));
    }

    public async Task WriteToJobSummaryAsync(GitHubConfiguration configuration, string title, string markdownBody)
    {
        var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

        await using var writer =
            new StreamWriter(summaryPath, Encoding.UTF8, new FileStreamOptions { Mode = FileMode.Append });

        await writer.WriteLineAsync($"## {title}");

        await writer.WriteLineAsync($"- ACTIONS_RUNTIME_URL: {Environment.GetEnvironmentVariable("ACTIONS_RUNTIME_URL")}");
        await writer.WriteLineAsync($"- ACTIONS_RUNTIME_TOKEN: {Environment.GetEnvironmentVariable("ACTIONS_RUNTIME_TOKEN")}");
        await writer.WriteLineAsync($"- ACTIONS_CACHE_URL: {Environment.GetEnvironmentVariable("ACTIONS_CACHE_URL")}");
        await writer.WriteLineAsync(Environment.NewLine + Environment.NewLine);

        // Normalize line endings in the body before printing it.
        await writer.WriteLineAsync(markdownBody.RegexReplace(@"\r?\n", Environment.NewLine));
        await writer.WriteLineAsync(Environment.NewLine + Environment.NewLine);
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
