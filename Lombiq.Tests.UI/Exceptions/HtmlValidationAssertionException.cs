using Atata.HtmlValidation;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class HtmlValidationAssertionException : Exception
    {
        public HtmlValidationResult HtmlValidationResult { get; }
        public HtmlValidationConfiguration HtmlValidationConfiguration { get; }

        public HtmlValidationAssertionException(
            HtmlValidationResult htmlValidationResult,
            HtmlValidationConfiguration validationConfiguration,
            Exception innerException)
            : base(
                validationConfiguration.CreateReportOnFailure
                    ? $"{innerException.Message} Check the HTML validation report in the failure dump for details."
                    : innerException.Message,
                innerException)
        {
            HtmlValidationResult = htmlValidationResult;
            HtmlValidationConfiguration = validationConfiguration;
        }

        public HtmlValidationAssertionException(string message)
            : base(message)
        {
        }

        public HtmlValidationAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public HtmlValidationAssertionException()
        {
        }
    }
}
