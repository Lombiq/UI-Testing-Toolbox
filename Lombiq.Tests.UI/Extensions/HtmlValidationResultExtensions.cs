using Atata.HtmlValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class HtmlValidationResultExtensions
    {
        public static async Task<IEnumerable<string>> GetErrorsAsync(this HtmlValidationResult result)
        {
            if (string.IsNullOrEmpty(result.ResultFilePath) || !File.Exists(result.ResultFilePath))
            {
                return Enumerable.Empty<string>();
            }

            var fullOutput = await File.ReadAllTextAsync(result.ResultFilePath);
            return fullOutput
                .Split(Environment.NewLine + Environment.NewLine + "error:", StringSplitOptions.RemoveEmptyEntries)
                .Select(error =>
                    (error.StartsWith("error:", StringComparison.OrdinalIgnoreCase) ? string.Empty : "error:") + error);
        }
    }
}
