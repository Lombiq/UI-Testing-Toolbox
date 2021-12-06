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
            HtmlValidationResult validationResult,
            HtmlValidationConfiguration validationConfiguration,
            Exception inner)
            : base(
                validationConfiguration.CreateReportOnFailure
                    ? $"{inner.Message} Check the HTML validation report in the failure dump for details."
                    : inner.Message,
                inner)
        {
            HtmlValidationResult = validationResult;
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
