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

        try
        {
            await configuration.AssertAppLogsAsync.InvokeFuncAsync(context.Application);
        }
        catch (Exception)
        {
            testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
            testOutputHelper.WriteLine(await context.Application.GetLogContentsAsync());

            throw;
        }

        if (context.IsBrowserRunning)
        {
            try
            {
                configuration.AssertResponseLog?.Invoke(context.CumulativeResponseLog);
                configuration.AssertBrowserLog?.Invoke(context.CumulativeBrowserLog);
            }
            catch (Exception)
            {
                testOutputHelper.WriteLine("Browser logs: " + Environment.NewLine);
                testOutputHelper.WriteLine(context.CumulativeBrowserLog.ToFormattedString());

                testOutputHelper.WriteLine("Response logs: " + Environment.NewLine);
                testOutputHelper.WriteLine(context.CumulativeResponseLog.ToFormattedString());

                throw;
            }
        }
    }
}
