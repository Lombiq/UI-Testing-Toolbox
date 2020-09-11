using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class WebApplicationInstanceExtensions
    {
        /// <summary>
        /// Asserting that the logs should be empty. When they aren't the Shouldly exception will contain the logs'
        /// contents.
        /// </summary>
        public static async Task LogsShouldBeEmptyAsync(this IWebApplicationInstance webApplicationInstance, bool canContainWarnings = false)
        {
            var logOutput = await webApplicationInstance.GetLogOutputAsync();

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
        public static async Task<string> GetLogOutputAsync(this IWebApplicationInstance webApplicationInstance) =>
            string.Join(Environment.NewLine + Environment.NewLine, await webApplicationInstance.GetLogs().ToFormattedStringAsync());
    }
}
