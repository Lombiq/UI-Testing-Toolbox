using Atata.Cli;
using Atata.HtmlValidation;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class HtmlValidationUITestContextExtensions
{
    /// <summary>
    /// Executes assertions on the result of an HTML markup validation with the html-validate library. Note that you
    /// need to run this after every page load, it won't accumulate during a session.
    /// </summary>
    /// <param name="assertHtmlValidationResultAsync">
    /// The assertion logic to run on the result of an HTML markup validation. If <see langword="null"/> then the
    /// assertion supplied in the context will be used.
    /// </param>
    /// <param name="htmlValidationOptionsAdjuster">
    /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
    /// </param>
    public static async Task AssertHtmlValidityAsync(
        this UITestContext context,
        Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null,
        Func<HtmlValidationResult, Task> assertHtmlValidationResultAsync = null)
    {
        var validationResult = await context.ValidateHtmlAsync(htmlValidationOptionsAdjuster);
        var validationConfiguration = context.Configuration.HtmlValidationConfiguration;

        try
        {
            if (assertHtmlValidationResultAsync == null &&
                validationConfiguration.HtmlValidationFilters is { Count: > 0 } filters)
            {
                var errors = validationResult.GetParsedErrors()?.AsList() ?? [];
                if (errors.Count > 0) AssertHtmlValidityWithFilters(errors, filters);
            }
            else
            {
                // This is the only place where AssertHtmlValidationResultAsync should be used. When it's removed in the
                // future, replace "validationConfiguration.AssertHtmlValidationResultAsync" below with
                // "HtmlValidationConfiguration.AssertHtmlValidationOutputIsEmptyAsync" to maintain default behavior.
#pragma warning disable CS0618 // Type or member is obsolete.
                if ((assertHtmlValidationResultAsync ?? validationConfiguration.AssertHtmlValidationResultAsync) is { } assert &&
                    assert(validationResult) is { } assertTask)
                {
                    await assertTask;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        catch (Exception exception)
        {
            throw new HtmlValidationAssertionException(validationResult, validationConfiguration, exception);
        }
    }

    private static void AssertHtmlValidityWithFilters(
        IList<JsonHtmlValidationError> errors,
        IDictionary<string, Func<JsonHtmlValidationError, bool>> filters)
    {
        foreach (var filter in filters.Values.Where(filter => filter != null))
        {
            errors.RemoveAll(error => !filter(error));
            if (errors.Count == 0) return;
        }

        var humanReadableErrors = HtmlValidationResultExtensions.GetParsedErrorMessageString(errors);
        var filtersUsedMessage = $"The following {nameof(HtmlValidationConfiguration.HtmlValidationFilters)} were " +
            $"used: {string.Join(", ", filters.Keys)}";

        errors.ShouldBeEmpty($"{humanReadableErrors}\n\n{filtersUsedMessage}");
    }

    /// <summary>
    /// Runs an HTML markup validation with the html-validate library. Note that you need to run this after every page
    /// load, it won't accumulate during a session.
    /// </summary>
    /// <param name="htmlValidationOptionsAdjuster">
    /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
    /// </param>
    [Obsolete("Use ValidateHtmlAsync instead. This method will be removed in a future version.")]
    public static HtmlValidationResult ValidateHtml(
        this UITestContext context,
        Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null)
    {
        // Duplicating ValidateHtmlAsync is not nice, but still better to use the native sync HtmlValidator.Validate()
        // method than doing .Result on ValidateHtmlAsync.

        var options = context.Configuration.HtmlValidationConfiguration.HtmlValidationOptions.Clone();
        htmlValidationOptionsAdjuster?.Invoke(options);
        try
        {
            return new HtmlValidator(options).Validate(context.Driver.PageSource);
        }
        catch (CliCommandException exception) when (exception.Message.Contains("'EACCES'"))
        {
            throw new InvalidOperationException(
                "Permission error while trying to install \"html-validate\". This is likely an issue with your " +
                "NPM installation. See https://docs.npmjs.com/resolving-eacces-permissions-errors-when-installing-packages-globally " +
                "for information on how to resolve this problem.",
                exception);
        }
    }

    /// <summary>
    /// Runs an HTML markup validation with the html-validate library. Note that you need to run this after every page
    /// load, it won't accumulate during a session.
    /// </summary>
    /// <param name="htmlValidationOptionsAdjuster">
    /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
    /// </param>
    public static async Task<HtmlValidationResult> ValidateHtmlAsync(
        this UITestContext context,
        Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null)
    {
        var options = context.Configuration.HtmlValidationConfiguration.HtmlValidationOptions.Clone();
        htmlValidationOptionsAdjuster?.Invoke(options);
        try
        {
            return await new HtmlValidator(options).ValidateAsync(context.Driver.PageSource);
        }
        catch (CliCommandException exception) when (exception.Message.Contains("'EACCES'"))
        {
            throw new InvalidOperationException(
                "Permission error while trying to install \"html-validate\". This is likely an issue with your " +
                "NPM installation. See https://docs.npmjs.com/resolving-eacces-permissions-errors-when-installing-packages-globally " +
                "for information on how to resolve this problem.",
                exception);
        }
    }
}
