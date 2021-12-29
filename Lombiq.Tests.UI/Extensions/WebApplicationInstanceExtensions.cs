using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class WebApplicationInstanceExtensions
    {
        /// <summary>
        /// Asserting that the logs should be empty. When they aren't the Shouldly exception will contain the logs'
        /// contents.
        /// </summary>
        public static async Task LogsShouldBeEmptyAsync(
            this IWebApplicationInstance webApplicationInstance,
            bool canContainWarnings = false,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default) cancellationToken = CancellationToken.None;

            var logOutput = await webApplicationInstance.GetLogOutputAsync(cancellationToken);

            if (canContainWarnings)
            {
                logOutput.ShouldNotContain("|ERROR|");
                logOutput.ShouldNotContain("|FATAL|");
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
}
