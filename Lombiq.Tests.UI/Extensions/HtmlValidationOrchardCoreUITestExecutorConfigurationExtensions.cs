using Atata.HtmlValidation;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
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
        /// <param name="assertHtmlValidationResult">
        /// The assertion logic to run on the result of an HTML markup validation. If <see langword="null"/> then the
        /// assertion supplied in the context will be used.
        /// </param>
        /// <param name="htmlValidationOptionsAdjuster">
        /// A delegate to adjust the <see cref="HtmlValidationOptions"/> instance supplied in the context.
        /// </param>
        public static void SetUpHtmlValidationAssertionOnPageChange(
            this OrchardCoreUITestExecutorConfiguration configuration,
            Action<HtmlValidationOptions> htmlValidationOptionsAdjuster = null,
            Action<HtmlValidationResult> assertHtmlValidationResult = null)
        {
            static bool ShouldRun(UITestContext context)
            {
                var url = context.Driver.Url;
                return
                    url.Contains("localhost:", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith(
                        context.SmtpServiceRunningContext?.WebUIUri.ToString() ?? "dummy://",
                        StringComparison.OrdinalIgnoreCase) &&
                    !url.Contains("Lombiq.Tests.UI.Shortcuts", StringComparison.OrdinalIgnoreCase);
            }

            IWebElement html = null;

            configuration.Events.AfterNavigation += (context, targetUri) =>
            {
                if (ShouldRun(context))
                {
                    context.AssertHtmlValidity(htmlValidationOptionsAdjuster, assertHtmlValidationResult);
                }

                return Task.CompletedTask;
            };

            configuration.Events.BeforeClick += (context, targetElement) =>
            {
                html = context.Get(By.TagName("html"));
                return Task.CompletedTask;
            };

            configuration.Events.AfterClick += (context, targetElement) =>
            {
                if (ShouldRun(context))
                {
                    try
                    {
                        // A dummy access just to make Text throw an exception if the element is stale.
                        html.Text?.StartsWith("a", StringComparison.InvariantCulture);
                    }
                    catch (StaleElementReferenceException)
                    {
                        // The page changed so time to run the validation.
                        context.AssertHtmlValidity(htmlValidationOptionsAdjuster, assertHtmlValidationResult);
                    }
                }

                return Task.CompletedTask;
            };
        }
    }
}
