using Atata.HtmlValidation;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class HtmlValidationOrchardCoreUITestExecutorConfigurationExtensions
    {
        /// <summary>
        /// Sets up HTML validation to run every time a page changes (either due to explicit navigation or clicks) and
        /// asserts on the validation results.
        /// </summary>
        /// <param name="assertHtmlValidationResultAsync">
        /// The assertion logic to run on the result of an HTML markup validation. If <see langword="null"/> then the
        /// assertion supplied in the context will be used.
        /// </param>
        /// <param name="htmlValidationOptionsAdjuster">
        /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
        /// </param>
        public static void SetUpHtmlValidationAssertionOnPageChange(
            this OrchardCoreUITestExecutorConfiguration configuration,
            Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null,
            Func<HtmlValidationResult, Task> assertHtmlValidationResultAsync = null)
        {
            if (!configuration.CustomConfiguration.TryAdd("HtmlValidationAssertionOnPageChangeWasSetUp", value: true)) return;

            configuration.Events.AfterPageChange += async context =>
            {
                if (configuration.HtmlValidationConfiguration.HtmlValidationAndAssertionOnPageChangeRule?.Invoke(context) == true)
                {
                    await context.AssertHtmlValidityAsync(htmlValidationOptionsAdjuster, assertHtmlValidationResultAsync);
                }
            };
        }
    }
}
