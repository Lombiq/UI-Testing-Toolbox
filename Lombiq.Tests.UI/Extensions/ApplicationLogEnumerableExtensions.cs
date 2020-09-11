using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ApplicationLogEnumerableExtensions
    {
        public static async Task<string> ToFormattedStringAsync(this IEnumerable<IApplicationLog> logs) =>
            string.Join(
                Environment.NewLine + Environment.NewLine,
                await Task.WhenAll(logs.Select(async log => log.Name + Environment.NewLine + Environment.NewLine + await log.GetContentAsync())));
    }
}
