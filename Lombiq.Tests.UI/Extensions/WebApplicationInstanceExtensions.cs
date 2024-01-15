using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class WebApplicationInstanceExtensions
{
    /// <summary>
    /// Asserting that the logs should be empty. When they aren't the Shouldly exception will contain the logs'
    /// contents.
    /// </summary>
    /// <param name="permittedErrorLines">
    /// If not <see langword="null"/> or empty, each line is split and any lines containing <c>|ERROR|</c> will be
    /// ignored if they contain any string from this collection (case-insensitive).
    /// </param>
    public static async Task LogsShouldBeEmptyAsync(
        this IWebApplicationInstance webApplicationInstance,
        bool canContainWarnings = false,
        ICollection<string> permittedErrorLines = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;
        permittedErrorLines ??= Array.Empty<string>();

        var logOutput = await webApplicationInstance.GetLogOutputAsync(cancellationToken);

        if (canContainWarnings)
        {
            logOutput.ShouldNotContain("|FATAL|");

            var errorLines = logOutput
                .SplitByNewLines()
                .Where(line => line.Contains("|ERROR|"));

            if (permittedErrorLines.Any())
            {
                errorLines = errorLines.Where(line => !permittedErrorLines.Any(line.ContainsOrdinalIgnoreCase));
            }

            errorLines.ShouldBeEmpty();
        }
        else
        {
            logOutput.ShouldBeEmpty();
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
