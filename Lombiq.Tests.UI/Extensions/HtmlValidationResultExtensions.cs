using Atata.HtmlValidation;
using Lombiq.Tests.UI.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

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

    /// <summary>
    /// Gets the parsed errors from the HTML validation result.
    /// Can only be used if the output formatter is set to JSON.
    /// </summary>
    public static IEnumerable<JsonHtmlValidationError> GetParsedErrors(this HtmlValidationResult result) => ParseOutput(result.Output);

    public static string GetParsedErrorMessageString(IEnumerable<JsonHtmlValidationError> errors) =>
        string.Join(
            '\n', errors.Select(error =>
                $"{error.Line.ToString(CultureInfo.InvariantCulture)}:{error.Column.ToString(CultureInfo.InvariantCulture)} - " +
                $"{error.Message} - " +
                $"{error.RuleId}"));

    private static IEnumerable<JsonHtmlValidationError> ParseOutput(string output)
    {
        try
        {
            // In some cases the output is too large and is not a valid JSON anymore. In this case we need to fix it.
            // tracking issue: https://github.com/atata-framework/atata-htmlvalidation/issues/9
            int index = output.IndexOf(",\"source\":", StringComparison.Ordinal);
            if (index != -1)
            {
                output = output[..index];
                output += "}]";
            }

            var document = JsonDocument.Parse(output);
            return document.RootElement.EnumerateArray()
                .SelectMany(element => element.GetProperty("messages").EnumerateArray())
                .Select(message =>
                {
                    var rawMessageText = message.GetRawText();
                    return JsonSerializer.Deserialize<JsonHtmlValidationError>(rawMessageText);
                });
        }
        catch (JsonException exception)
        {
            throw new JsonException(
                $"Unable to parse output, was OutputFormatter set to JSON? Length: {output.Length} " +
                $"Output: {output}",
                exception);
        }
    }
}
