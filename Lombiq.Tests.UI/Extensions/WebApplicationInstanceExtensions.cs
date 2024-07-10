using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class WebApplicationInstanceExtensions
{
    /// <summary>
    /// Asserting that the logs should be empty. When they aren't the Shouldly exception will contain the logs'
    /// contents.
    /// </summary>
    /// <param name="permittedErrorLinePatterns">
    /// If not <see langword="null"/> or empty, each line is split and any lines containing <c>|ERROR|</c> will be
    /// ignored if they regex match any string from this collection (case-insensitive).
    /// </param>
    public static async Task LogsShouldBeEmptyAsync(
        this IWebApplicationInstance webApplicationInstance,
        bool canContainWarnings = false,
        ICollection<string> permittedErrorLinePatterns = null,
        CancellationToken cancellationToken = default)
    {
        permittedErrorLinePatterns ??= [];

        var logOutput = await webApplicationInstance.GetLogOutputAsync(cancellationToken);

        logOutput.ShouldNotContain("|FATAL|");

        var lines = logOutput.SplitByNewLines();

        var errorLines = lines.Where(line => line.Contains("|ERROR|"));

        if (permittedErrorLinePatterns.Count != 0)
        {
            errorLines = errorLines.Where(line =>
                !permittedErrorLinePatterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)));
        }

        errorLines.ShouldBeEmpty();

        if (!canContainWarnings)
        {
            lines.Where(line => line.Contains("|WARNING|")).ShouldBeEmpty();
        }
    }

    /// <summary>
    /// Retrieves all the logs and concatenates them into a single formatted string.
    /// </summary>
    public static async Task<string> GetLogOutputAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            await webApplicationInstance.GetLogs(cancellationToken).ToFormattedStringAsync());
    }
}
