using Atata.HtmlValidation;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class HtmlValidationAssertionException : Exception
    {
        public HtmlValidationResult HtmlValidationResult { get; }

        public HtmlValidationAssertionException(
            HtmlValidationResult htmlValidationResult,
            bool createReportOnFailure,
            Exception innerException)
            : base(
                "Asserting the HTML validation result failed." +
                  (createReportOnFailure ? " Check the HTML validation report in the failure dump for details." : string.Empty),
                innerException) =>
            HtmlValidationResult = htmlValidationResult;

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
    }
}
