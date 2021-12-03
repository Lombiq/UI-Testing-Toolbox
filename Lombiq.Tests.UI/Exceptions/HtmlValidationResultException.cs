using Atata.HtmlValidation;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class HtmlValidationResultException : Exception
    {
        public HtmlValidationResult HtmlValidationResult { get; set; }

        public HtmlValidationResultException(HtmlValidationResult validationResult, Exception inner)
            : base(inner.Message, inner) =>
            HtmlValidationResult = validationResult;

        public HtmlValidationResultException(string message)
            : base(message)
        {
        }

        public HtmlValidationResultException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public HtmlValidationResultException()
        {
        }
    }
}
