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
    /// Verifies all logs and throws an exception if they didn't pass the checks.
    /// </summary>
    public static async Task AssertLogsAsync(this UITestContext context)
    {
        var configuration = context.Configuration;
        var testOutputHelper = configuration.TestOutputHelper;

        if (context.IsBrowserRunning) await context.UpdateHistoricBrowserLogAsync();

        try
        {
            await configuration.AssertAppLogsAsync.InvokeFuncAsync(context.Application);
        }
        catch (Exception)
        {
            testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
            testOutputHelper.WriteLine(await context.Application.GetLogOutputAsync());

            throw;
        }

        if (context.IsBrowserRunning)
        {
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
}
