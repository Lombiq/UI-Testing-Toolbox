using Atata.HtmlValidation;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class HtmlValidationAssertionException : Exception
    {
        public HtmlValidationResult HtmlValidationResult { get; }
        public Uri Address { get; set; }
        public string Title { get; set; }

        public HtmlValidationAssertionException(
            HtmlValidationResult htmlValidationResult,
            UITestContext context,
            bool createReportOnFailure,
            Exception innerException)
            : base(CreateErrorMessage(context, createReportOnFailure), innerException)
        {
            HtmlValidationResult = htmlValidationResult;
            Address = new Uri(context.Driver.Url);
            Title = context.Driver.Title;
        }

        public HtmlValidationAssertionException()
        {
        }

        public HtmlValidationAssertionException(string message)
            : base(message)
        {
        }

        public HtmlValidationAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private static string CreateErrorMessage(UITestContext context, bool createReportOnFailure)
        {
            var url = context.Driver.Url;
            var title = context.Driver.Title ?? url;

            var message = $"Asserting the HTML validation result on page {url}({title}) failed.";

            return createReportOnFailure
                ? message + " Check the HTML validation report in the failure dump for details."
                : message;
        }
    }
}
