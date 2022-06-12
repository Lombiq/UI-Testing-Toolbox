using System;

namespace Lombiq.Tests.UI.Exceptions;

public class VisualVerificationSourceInformationNotAvailableException : Exception
{
    public VisualVerificationSourceInformationNotAvailableException(string message)
        : base(message)
    {
    }

    public VisualVerificationSourceInformationNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public VisualVerificationSourceInformationNotAvailableException()
    {
    }
}
