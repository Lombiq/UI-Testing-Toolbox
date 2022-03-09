using Atata.HtmlValidation;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
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
            var validationResult = context.ValidateHtml(htmlValidationOptionsAdjuster);
            var validationConfiguration = context.Configuration.HtmlValidationConfiguration;

            try
            {
                var assertTask = (assertHtmlValidationResultAsync ?? validationConfiguration.AssertHtmlValidationResultAsync)?
                    .Invoke(validationResult);
                await (assertTask ?? Task.CompletedTask);
            }
            catch (Exception exception)
            {
                throw new HtmlValidationAssertionException(validationResult, validationConfiguration, exception);
            }
        }

        /// <summary>
        /// Runs an HTML markup validation with the html-validate library. Note that you need to run this after every
        /// page load, it won't accumulate during a session.
        /// </summary>
        /// <param name="htmlValidationOptionsAdjuster">
        /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
        /// </param>
        public static HtmlValidationResult ValidateHtml(
            this UITestContext context,
            Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null)
        {
            var options = context.Configuration.HtmlValidationConfiguration.HtmlValidationOptions.Clone();
            htmlValidationOptionsAdjuster?.Invoke(options);
            return new HtmlValidator(options).Validate(context.Driver.PageSource);
        }
    }
}
