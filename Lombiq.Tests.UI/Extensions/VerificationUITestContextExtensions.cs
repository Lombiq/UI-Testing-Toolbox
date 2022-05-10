using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class VerificationUITestContextExtensions
{
    /// <summary>
    /// Returns a <see cref="PageNavigationState"/> of the current page in the <paramref name="context"/>.
    /// </summary>
    public static PageNavigationState AsPageNavigationState(this UITestContext context) => new(context);

    /// <summary>
    /// Ensures all the logs pass successfully and throws if they didn't.
    /// </summary>
    public static async Task AssertLogsAsync(this UITestContext context)
    {
        var configuration = context.Configuration;
        var testOutputHelper = configuration.TestOutputHelper;

        await context.UpdateHistoricBrowserLogAsync();

        try
        {
            if (configuration.AssertAppLogsAsync != null) await configuration.AssertAppLogsAsync(context.Application);
        }
        catch (Exception)
        {
            testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
            testOutputHelper.WriteLine(await context.Application.GetLogOutputAsync());

            throw;
        }

        try
        {
            configuration.AssertBrowserLog?.Invoke(context.HistoricBrowserLog);
        }
        catch (Exception)
        {
            testOutputHelper.WriteLine("Browser logs: " + Environment.NewLine);
            testOutputHelper.WriteLine(context.HistoricBrowserLog.ToFormattedString());

            throw;
        }
    }
}
